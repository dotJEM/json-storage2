using System;
using System.Threading.Tasks;

namespace DotJEM.Json.Storage2;

public interface IStorageArea<TJson>
{
    string Name { get; }

    IStorageAreaLog<TJson> Log { get; }

    IAsyncEnumerable<StorageObject<TJson>> GetAsync();
    IAsyncEnumerable<StorageObject<TJson>> GetAsync(CancellationToken cancellation);
    IAsyncEnumerable<StorageObject<TJson>> GetAsync(long skip);
    IAsyncEnumerable<StorageObject<TJson>> GetAsync(long skip, CancellationToken cancellation);
    IAsyncEnumerable<StorageObject<TJson>> GetAsync(long skip, int take, CancellationToken cancellation);

    Task<StorageObject<TJson>?> GetAsync(Guid id);
    Task<StorageObject<TJson>?> GetAsync(Guid id, CancellationToken cancellation);
    Task<StorageObject<TJson>> InsertAsync(string contentType, TJson obj);
    Task<StorageObject<TJson>> InsertAsync(string contentType, TJson obj, CancellationToken cancellation);
    Task<StorageObject<TJson>> InsertAsync(InsertStorageObject<TJson> obj);
    Task<StorageObject<TJson>> InsertAsync(InsertStorageObject<TJson> obj, CancellationToken cancellation);
    Task<StorageObject<TJson>> UpdateAsync(Guid id, TJson obj);
    Task<StorageObject<TJson>> UpdateAsync(Guid id, TJson obj, CancellationToken cancellation);
    Task<StorageObject<TJson>> UpdateAsync(UpdateStorageObject<TJson> obj);
    Task<StorageObject<TJson>> UpdateAsync(UpdateStorageObject<TJson> obj, CancellationToken cancellation);
    Task<StorageObject<TJson>?> DeleteAsync(Guid id);
    Task<StorageObject<TJson>?> DeleteAsync(Guid id, CancellationToken cancellation);
}

public readonly record struct ChangeCount(int Created, int Updated, int Deleted)
{
    public int Total { get; } = Created + Updated + Deleted;

    public static ChangeCount operator +(ChangeCount left, ChangeCount right)
    {
        return new ChangeCount(
            left.Created + right.Created,
            left.Updated + right.Updated,
            left.Deleted + right.Deleted);
    }

    public static implicit operator int(ChangeCount count)
    {
        return count.Total;
    }

    public override string ToString()
    {
        return $"Created: {Created}, Updated: {Updated}, Deleted: {Deleted}";
    }
}

public interface IStorageAreaChangeCollection<TJson>
{

}

public interface IStorageAreaLog<TJson>
{
    /// <summary>
    /// Gets the latest generation returned by this changelog.
    /// </summary>
    long CurrentGeneration { get; }

    /// <summary>
    /// Gets the latest generation stored in the database.
    /// </summary>
    Task<long> GetLatestGeneration();

    /// <summary>
    /// Gets the next batch of changes.
    /// </summary>
    /// <remarks>
    /// Use this method to continiously pool for changed documents in the storage area while letting the <see cref="IStorageAreaLog"/> track which generation was returned last.
    /// </remarks>
    /// <param name="includeDeletes">If <code>true</code>, returns all types of changes; If <code>false</code>, it skips deletes.</param>
    /// <param name="count">The maximum number of changes to return.</param>
    Task<IStorageAreaChangeCollection<TJson>> Get(int count = 5000, bool includeDeletes = true);

    /// <summary>
    /// Gets a batch of changes from the provided <see cref="generation"/>.
    /// </summary>
    /// <remarks>
    /// Use this method to continiously pool for changed documents in the storage area while taking over tracking of the last returned generation.
    /// <strong>Note:</strong>If <see cref="count"/> is less than <code>1</code>, then this method will just reset <see cref="CurrentGeneration"/> to
    /// the <see cref="generation"/> provided unless the <see cref="generation"/> provided is greater than <see cref="LatestGeneration"/>, in which case
    /// <see cref="CurrentGeneration"/> is set to <see cref="LatestGeneration"/>.
    /// </remarks>
    /// <param name="generation">The generation to start from.</param>
    /// <param name="includeDeletes">If <code>true</code>, returns all types of changes; If <code>false</code>, it skips deletes.</param>
    /// <param name="count">The maximum number of changes to return.</param>
    /// <returns></returns>
    Task<IStorageAreaChangeCollection<TJson>> Get(long generation, int count = 5000, bool includeDeletes = true);

    IStorageAreaLogObserver<TJson> OpenOberver();

}

public interface IStorageAreaLogObserver<TJson>
{

}