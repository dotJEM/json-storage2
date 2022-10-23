using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Storage2;

public interface IStorageArea
{
    string Name { get; }

    IAsyncEnumerable<StorageObject> GetAsync();
    IAsyncEnumerable<StorageObject> GetAsync(long skip, int take = 100);

    Task<StorageObject?> GetAsync(Guid id);
    Task<StorageObject> InsertAsync(string contentType, JObject obj);
    Task<StorageObject> InsertAsync(StorageObject obj);
    Task<StorageObject> UpdateAsync(Guid id, JObject obj);
    Task<StorageObject> UpdateAsync(StorageObject obj);
    Task<StorageObject?> DeleteAsync(Guid id);
}