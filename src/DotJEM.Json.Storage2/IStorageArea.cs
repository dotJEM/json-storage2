using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Storage2;

public interface IStorageArea
{
    string Name { get; }

#if NETSTANDARD2_0
    Task<IEnumerable<StorageObject>> GetAsync();
    Task<IEnumerable<StorageObject>> GetAsync(long skip, int take = 100);
#else
    IAsyncEnumerable<StorageObject> GetAsync();
    IAsyncEnumerable<StorageObject> GetAsync(long skip, int take = 100);
#endif

    Task<StorageObject?> GetAsync(Guid id);
    Task<StorageObject> InsertAsync(string contentType, JObject obj);
    Task<StorageObject> InsertAsync(StorageObject obj);
    Task<StorageObject> UpdateAsync(Guid id, JObject obj);
    Task<StorageObject> UpdateAsync(StorageObject obj);
    Task<StorageObject?> DeleteAsync(Guid id);
}