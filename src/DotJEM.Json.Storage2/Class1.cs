using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using DotJEM.Json.Storage2.Cache;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Storage2;



public interface IStorageContext { }

public class SqlServerStorageContext : IStorageContext
{
    private readonly string connectionString;
    private readonly AsyncCache<SqlServerStorageArea> areas = new();
    private readonly SqlServerStorageAreaFactory factory;

    public SqlServerStorageContext(string connectionString)
    {
        this.connectionString = connectionString;
        factory = new SqlServerStorageAreaFactory(this);
    }

    public async Task<IStorageArea> AreaAsync(string name)
    {
        return await areas.GetOrAdd(name, key => factory.Create(key));
    }

    public bool Release(string name)
    {
        return areas.Release(name);
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(connectionString);
    }
}

public interface IStorageArea
{
    string Name { get; }

    IAsyncEnumerable<StorageObject> GetAsync();
    IAsyncEnumerable<StorageObject> GetAsync(long skip, int take = 100);

    Task<StorageObject> GetAsync(Guid id);
    Task<StorageObject> InsertAsync(StorageObject obj);
    Task<StorageObject> UpdateAsync(Guid id, StorageObject obj);
    Task<StorageObject> DeleteAsync(Guid id);
}

public class SqlServerStorageArea : IStorageArea
{
    public string Name { get; }

    public SqlServerStorageArea(SqlServerStorageContext sqlServerStorageContext, string name)
    {
        this.Name = name;
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

public readonly record struct StorageObject(Guid Id, int Version, DateTime Created, DateTime Updated, JObject Data);