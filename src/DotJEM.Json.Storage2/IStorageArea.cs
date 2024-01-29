using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Storage2;

public interface IStorageArea
{
    string Name { get; }

#if NETSTANDARD2_0
    //Task<IEnumerable<StorageObject>> GetAsync();
    //Task<IEnumerable<StorageObject>> GetAsync(long skip, int take = 100);
#else
    //IAsyncEnumerable<StorageObject> GetAsync();
    //IAsyncEnumerable<StorageObject> GetAsync(CancellationToken cancellation);
    //IAsyncEnumerable<StorageObject> GetAsync(long skip);
    //IAsyncEnumerable<StorageObject> GetAsync(long skip, CancellationToken cancellation);
    //IAsyncEnumerable<StorageObject> GetAsync(long skip, int take, CancellationToken cancellation);
#endif

    Task<StorageObject?> GetAsync(Guid id);
    Task<StorageObject?> GetAsync(Guid id, CancellationToken cancellation);
    Task<StorageObject> InsertAsync(string contentType, JObject obj);
    Task<StorageObject> InsertAsync(string contentType, JObject obj, CancellationToken cancellation);
    Task<StorageObject> InsertAsync(InsertStorageObject obj);
    Task<StorageObject> InsertAsync(InsertStorageObject obj, CancellationToken cancellation);
    Task<StorageObject> UpdateAsync(Guid id, JObject obj);
    Task<StorageObject> UpdateAsync(Guid id, JObject obj, CancellationToken cancellation);
    Task<StorageObject> UpdateAsync(UpdateStorageObject obj);
    Task<StorageObject> UpdateAsync(UpdateStorageObject obj, CancellationToken cancellation);
    Task<StorageObject?> DeleteAsync(Guid id);
    Task<StorageObject?> DeleteAsync(Guid id, CancellationToken cancellation);
}