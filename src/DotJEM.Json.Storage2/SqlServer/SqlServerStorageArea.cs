using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Storage2.SqlServer;

public class SqlServerStorageArea : IStorageArea
{
    private readonly SqlServerAreaStateManager stateManager;
    private readonly SqlServerStorageContext context;

    public string Name { get; }

    public SqlServerStorageArea(SqlServerStorageContext context, SqlServerAreaStateManager stateManager)
    {
        this.context = context;
        this.stateManager = stateManager;
    }

    public async IAsyncEnumerable<StorageObject> GetAsync()
    {
        if (!stateManager.Exists)
            yield break;




        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<StorageObject> GetAsync(long skip, int take = 100)
    {
        if (!stateManager.Exists)
            yield break;

        throw new NotImplementedException();
    }

    public async Task<StorageObject?> GetAsync(Guid id)
    {
        if (!stateManager.Exists)
            return null;

        throw new NotImplementedException();
    }

    public Task<StorageObject> InsertAsync(JObject obj)
    {
        return InsertAsync(new StorageObject(Guid.Empty, 0, DateTime.MinValue, DateTime.MinValue, obj));
    }

    public async Task<StorageObject> InsertAsync(StorageObject obj)
    {
        await stateManager.Ensure();


        throw new NotImplementedException();
    }



    public Task<StorageObject> UpdateAsync(Guid id, StorageObject obj)
    {
        throw new NotImplementedException();
    }
    public Task<StorageObject> UpdateAsync(Guid id, JObject obj)
    {
        throw new NotImplementedException();
    }

    public Task<StorageObject?> DeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }


}