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
            (StorageEvent)row.GetString("Event")[0],
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

        if (changes.Count == 0)
            return new ChangeCollection<TJson>([]);

        CurrentGeneration = changes.Last().Revision;
        return new ChangeCollection<TJson>([.. changes]);
    }

    public IStorageAreaLogObserver<TJson> OpenObserver(int batchSize = 5000)
        => OpenObserver(CurrentGeneration, batchSize);


    public IStorageAreaLogObserver<TJson> OpenObserver(long generation, int batchSize = 5000)
    {
        return new StorageAreaLogObserver<TJson>(this, generation, batchSize);
    }
}

public record ChangeCollection<TJson> : IChangeCollection<TJson>
{
    private readonly IImmutableList<StorageChange<TJson>> changes;

    public int Count => changes.Count;

    public long LatestRevision => changes[^1].Revision;

    public StorageChange<TJson> this[int index] => changes[index];
    public ChangeCollection(IImmutableList<StorageChange<TJson>> changes)
    {
        this.changes = changes;
    }

    public StorageChange<TJson>[] ToArray() => changes.ToArray();

    public IEnumerator<StorageChange<TJson>> GetEnumerator() => changes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


public class StorageAreaLogObserver<TJson>(SqlServerStorageAreaLog<TJson> log, long generation = 0, int batchSize = 5000) : IStorageAreaLogObserver<TJson>
{
    private bool isNotifying = false;
    private readonly Guid Id = Guid.NewGuid();
    private Task runner;
    private readonly object padlock = new { };
    private CancellationTokenSource cts;
    private long CurrentGeneration = generation;
    private readonly ConcurrentDictionary<Guid, Subscription> observers = new();
    private readonly ConcurrentDictionary<Guid, Subscription> incomming = new();

    public IDisposable Subscribe(IObserver<IChangeCollection<TJson>> observer)
    {
        if (observer == null) throw new ArgumentNullException(nameof(observer));
        Subscription subscription = new(this, observer, generation);
        incomming.TryAdd(subscription.Id, subscription);

        //TODO: Move to running a single loop?.
        NotifyObservers();
        return subscription;
    }

    private void NotifyObservers()
    {
        if (isNotifying)
            return;

        lock (padlock)
        {
            if (isNotifying)
                return;
            isNotifying = true;

            cts = new CancellationTokenSource();
            runner = Task.Run(() => Execute(cts.Token));
        }
    }

    private async Task Execute(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if(observers.IsEmpty && incomming.IsEmpty)
            {
                isNotifying = false;
                return;
            }

            try
            {
                await Poll();
                await Task.Delay(2000, token);
            }
            catch (OperationCanceledException) when (cts?.IsCancellationRequested == true)
            {
                isNotifying = false;
                return;
            }
        }
    }

    private async Task Poll()
    {
        while (!incomming.IsEmpty)
        {
            // TODO: Optimize this, it still has a bunch of optimizations that can be done.
            //       Primarily when we add multiple subscriptions shortly after each other they might only
            //       be slightly missaligned causing us to fetch the same changes multiple times.
            foreach (Subscription subscription in incomming.Values)
            {
                if (subscription.Generation >= CurrentGeneration)
                {
                    incomming.TryRemove(subscription.Id, out _);
                    observers.TryAdd(subscription.Id, subscription);
                    continue;
                }

                long maxBatchSize = Math.Max(batchSize, (subscription.Generation - CurrentGeneration));
                IChangeCollection<TJson> changes = await log.Get(subscription.Generation, (int)maxBatchSize);
                subscription.Notify(changes);
                CurrentGeneration = changes.LatestRevision;
            }
        }

        while (true)
        {
            IChangeCollection<TJson> changes = await log.Get(CurrentGeneration, batchSize);
            if(changes.Count == 0)
                break;

            foreach (Subscription subscription in observers.Values.OrderBy(sub => sub.Generation))
                subscription.Notify(changes);

            CurrentGeneration = changes.LatestRevision;
        }
    }

    private void Unsubscribe(Subscription subscription)
    {
        observers.TryRemove(subscription.Id, out _);
        if (observers.IsEmpty)
            cts?.Cancel();
    }

    private sealed class Subscription(
        StorageAreaLogObserver<TJson> owner,
        IObserver<IChangeCollection<TJson>> observer,
        long generation) : IDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();
        public long Generation { get; private set; } = generation;
        public bool Disposed;
        public void Notify(IChangeCollection<TJson> batch)
        {
            if (Disposed) return;
            Generation = batch.LatestRevision;
            try { observer.OnNext(batch); }
            catch (Exception ex) { Fail(ex); }
        }

        public void Fail(Exception ex)
        {
            if (Disposed) return;
            observer.OnError(ex);
            Dispose();
        }

        public void Dispose()
        {
            owner.Unsubscribe(this);
            Disposed = true;
        }
    }
}