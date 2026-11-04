using DotJEM.Json.Storage2.SqlServer.Initialization;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace DotJEM.Json.Storage2.SqlServer;

public class SqlServerStorageAreaLog<TJson>(
    SqlServerStorageContext<TJson> context,
    SqlServerAreaStateManager stateManager) : IStorageAreaLog<TJson>
{
    public long CurrentGeneration { get; private set; }

    public async Task<long> GetLatestGeneration()
    {
        if (!stateManager.Exists)
            return 0;

        using ISqlServerCommand cmd = context.CommandFactory.Create(
            SqlTemplates.SelectFromChangeLogTable_LatestGeneration(stateManager.Schema, stateManager.AreaName));
        return await cmd.ExecuteScalarAsync<long>(CancellationToken.None);
    }

    public Task<IChangeCollection<TJson>> Get(int count = 5000, bool includeDeletes = true)
        => Get(CurrentGeneration, count, includeDeletes);

    public async Task<IChangeCollection<TJson>> Get(long generation, int count = 5000, bool includeDeletes = true)
    {
        if (!stateManager.Exists)
            return new ChangeCollection<TJson>([]);

        using ISqlServerCommand cmd = context.CommandFactory.Create(
            SqlTemplates.SelectFromChangeLogTable_Paged(stateManager.Schema, stateManager.AreaName),
            ("count", count),
            ("start", generation));

        ISqlServerDataReader<StorageChange<TJson>> read = await cmd.ExecuteReaderAsync(row => new StorageChange<TJson>(
            row.GetInt64("Revision"),
            row.GetString("Event")[0],
            new StorageObject<TJson>(
                row.GetString("ContentType"),
                row.GetGuid("Id"),
                row.GetInt32("Version"),
                row.GetDateTime("Created"),
                row.GetDateTime("Updated"),
                row.GetString("CreatedBy"),
                row.GetString("UpdatedBy"),
                context.JsonConverter.Parse(row.GetString("Data"))
                )
        ), CancellationToken.None);

        List<StorageChange<TJson>> changes = new(count);
        await foreach (StorageChange<TJson> obj in read)
            changes.Add(obj);


        if (changes.Count > 0)
            CurrentGeneration = changes[^1].Revision;
        return new ChangeCollection<TJson>([.. changes]);
    }

    public IStorageAreaLogObserver<TJson> OpenOberver(long generation = 0, int batchSize = 5000)
    {
        return new StorageAreaLogObserver<TJson>(this, generation, batchSize);
    }
}

public readonly record struct ChangeCollection<TJson> : IChangeCollection<TJson>
{
    private readonly IImmutableList<StorageChange<TJson>> changes;

    public ChangeCollection(IImmutableList<StorageChange<TJson>> changes)
    {
        this.changes = changes;
    }


    public IEnumerator<StorageChange<TJson>> GetEnumerator() => changes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


public class StorageAreaLogObserver<TJson>(
    SqlServerStorageAreaLog<TJson> log,
    long generation = 0,
    int batchSize = 5000,
    TimeSpan? pollInterval = null) : IStorageAreaLogObserver<TJson>
{
    private readonly TimeSpan interval = pollInterval ?? TimeSpan.FromSeconds(1);
    private readonly object padlock = new();
    private readonly List<Subscription> subscriptions = new();
    private CancellationTokenSource? cts;
    private SemaphoreSlim? wake;

    public IDisposable Subscribe(IObserver<IChangeCollection<TJson>> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        Subscription subscription = new(this, observer, generation);
        SemaphoreSlim signal;
        lock (padlock)
        {
            subscriptions.Add(subscription);
            if (cts is null)
            {
                cts = new CancellationTokenSource();
                wake = new SemaphoreSlim(0);
                CancellationToken token = cts.Token;
                SemaphoreSlim started = wake;
                _ = Task.Run(() => RunAsync(token, started));
            }
            signal = wake!;
        }
        signal.Release();
        return subscription;
    }

    private async Task RunAsync(CancellationToken token, SemaphoreSlim signal)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!await PollAsync())
                    await signal.WaitAsync(interval, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                Subscription[] failed;
                lock (padlock)
                {
                    failed = subscriptions.ToArray();
                    subscriptions.Clear();
                    cts?.Cancel();
                    cts = null;
                }
                foreach (Subscription s in failed)
                    s.Fail(ex);
                return;
            }
        }
    }

    // Subscribers at the same generation share a single fetch. Returns true if more data is likely available.
    private async Task<bool> PollAsync()
    {
        Subscription[] current;
        lock (padlock)
            current = subscriptions.ToArray();

        bool more = false;
        foreach (IGrouping<long, Subscription> group in current.GroupBy(s => s.Generation))
        {
            IChangeCollection<TJson> changes = await log.Get(group.Key, batchSize);
            StorageChange<TJson>[] items = changes.ToArray();
            if (items.Length == 0)
                continue;

            long last = items[^1].Revision;
            more |= items.Length >= batchSize;
            foreach (Subscription subscription in group)
                subscription.Notify(changes, last);
        }
        return more;
    }

    private void Remove(Subscription subscription)
    {
        lock (padlock)
        {
            subscription.Disposed = true;
            subscriptions.Remove(subscription);
            if (subscriptions.Count == 0)
            {
                cts?.Cancel();
                cts = null;
            }
        }
    }

    private sealed class Subscription(
        StorageAreaLogObserver<TJson> owner,
        IObserver<IChangeCollection<TJson>> observer,
        long generation) : IDisposable
    {
        public long Generation { get; private set; } = generation;
        public volatile bool Disposed;

        public void Notify(IChangeCollection<TJson> batch, long last)
        {
            if (Disposed) return;
            Generation = last;
            try { observer.OnNext(batch); }
            catch (Exception ex) { Fail(ex); }
        }

        public void Fail(Exception ex)
        {
            if (Disposed) return;
            Disposed = true;
            observer.OnError(ex);
            owner.Remove(this);
        }

        public void Dispose() => owner.Remove(this);
    }
}
