using System.Data.SqlClient;
using DotJEM.Json.Storage2.Cache;

namespace DotJEM.Json.Storage2.SqlServer;

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