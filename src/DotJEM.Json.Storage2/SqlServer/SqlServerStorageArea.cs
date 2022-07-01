namespace DotJEM.Json.Storage2.SqlServer;

public class SqlServerStorageArea : IStorageArea
{
    public string Name { get; }

    public SqlServerStorageArea(SqlServerStorageContext sqlServerStorageContext, string name)
    {
        Name = name;
    }

    public IAsyncEnumerable<StorageObject> GetAsync()
    {

        throw new NotImplementedException();
    }

    public IAsyncEnumerable<StorageObject> GetAsync(long skip, int take = 100)
    {
        throw new NotImplementedException();
    }

    public Task<StorageObject> GetAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<StorageObject> InsertAsync(StorageObject obj)
    {
        throw new NotImplementedException();
    }

    public Task<StorageObject> UpdateAsync(Guid id, StorageObject obj)
    {
        throw new NotImplementedException();
    }

    public Task<StorageObject> DeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }


}