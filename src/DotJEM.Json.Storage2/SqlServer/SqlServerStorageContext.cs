using System.Data.Common;
using System.Data.SqlClient;
using DotJEM.Json.Storage2.Cache;

namespace DotJEM.Json.Storage2.SqlServer;

public class SqlServerStorageContext : IStorageContext
{
    public static async Task<SqlServerStorageContext> Create(string connectionString, string schema = "dbo")
    {
        SqlServerConnectionFactory connectionFactory = new SqlServerConnectionFactory(connectionString);
        SqlServerStorageAreaFactory areaFactory = await Temp.Create(schema, connectionFactory);
        return new SqlServerStorageContext(connectionFactory, areaFactory);
    } 

    private readonly AsyncCache<SqlServerStorageArea> areas = new();
    private readonly SqlServerStorageAreaFactory areaFactory;

    public ISqlServerConnectionFactory ConnectionFactory { get; }

    private SqlServerStorageContext(SqlServerConnectionFactory connectionFactory, SqlServerStorageAreaFactory areaFactory)
    {
        this.areaFactory = areaFactory;
        ConnectionFactory = connectionFactory;
    }

    public async Task<IStorageArea> AreaAsync(string name)
    {
        return await areas.GetOrAdd(name, key => areaFactory.Create(key, this));
    }

    public bool Release(string name)
    {
        return areas.Release(name);
    }

    public SqlConnection CreateConnection() => ConnectionFactory.Create();
}

public class SqlServerConnectionFactory : ISqlServerConnectionFactory
{
    private readonly string connectionString;

    public SqlServerConnectionFactory(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public SqlConnection Create()
    {
        return new SqlConnection(connectionString);
    }
}