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

        ISqlServerDataReader<StorageChange<TJson>> read = await cmd
            .ExecuteReaderAsync(
                ["Revision", "Id", "Event", "Time", "User", "Version", "Data"],
                values => new StorageChange<TJson>(
                    (long)values[0],
                    (Guid)values[1],
                    ((string)values[2])[0],
                    (int)values[5],
                    (DateTime)values[3],
                    (string)values[4],
                    context.JsonConverter.Parse((string)values[6])
                ),
                CancellationToken.None);

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
    private readonly ConcurrentDictionary<Guid, Subscription> observers = new();

    public IDisposable Subscribe(IObserver<IChangeCollection<TJson>> observer)
    {
        Subscription subscription = new( observer, Unsubscribe);
        observers.TryAdd(subscription.Id, subscription);
        NotifyObservers();
        return subscription;
    }


    private void Unsubscribe(Guid id)
    {
        observers.TryRemove(id, out _);
    }

    private async Task NotifyObservers()
    {
        foreach (Subscription subscription in observers.Values)
        {
            
        }
    }

    private class Subscription : IDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();
        private readonly IObserver<IChangeCollection<TJson>> observer;
        private readonly Action<Guid> unsubscribe;

        public Subscription(IObserver<IChangeCollection<TJson>> observer, Action<Guid> unsubscribe)
        {
            this.observer = observer;
            this.unsubscribe = unsubscribe;
        }

        public void Dispose() => unsubscribe(Id);
    }
}