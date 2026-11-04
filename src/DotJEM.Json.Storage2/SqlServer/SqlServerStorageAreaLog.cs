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
                context.JsonConverter.Parse(row.GetString("Data")                )
                )
        ), CancellationToken.None);

        List<StorageChange<TJson>> changes = new(count);
        await foreach (StorageChange<TJson> obj in read)
            changes.Add(obj);


        CurrentGeneration = changes.Last().Revision;
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


public class StorageAreaLogObserver<TJson>(SqlServerStorageAreaLog<TJson> log, long generation = 0, int batchSize = 5000) : IStorageAreaLogObserver<TJson>
{
    private bool isNotifying = false;
    private Task runner;
    private readonly object padLock = new { };
    private readonly ConcurrentDictionary<Guid, Subscription> observers = new();

    public IDisposable Subscribe(IObserver<IChangeCollection<TJson>> observer)
    {
        Subscription subscription = new(observer, Unsubscribe);
        observers.TryAdd(subscription.Id, subscription);

        //TODO: Move to running a single loop?.
        NotifyObservers();
        return subscription;
    }


    private void Unsubscribe(Guid id)
    {
        observers.TryRemove(id, out _);
    }

    private void Unsubscribe(Subscription subscription)
    {
        observers.TryRemove(subscription.Id, out _);
    }

    private void NotifyObservers()
    {
        if (isNotifying)
            return;

        lock (padLock)
        {
            if (isNotifying)
                return;

            runner = Task.Run(Execute);
            isNotifying = true;
        }

        async Task Execute()
        {
            while (!observers.IsEmpty)
            {

            }

            isNotifying = false;
        }
    }

    private sealed class Subscription(
        StorageAreaLogObserver<TJson> owner,
        IObserver<IChangeCollection<TJson>> observer,
        long generation) : IDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();
        public long Generation { get; private set; } = generation;
        public bool Disposed;
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