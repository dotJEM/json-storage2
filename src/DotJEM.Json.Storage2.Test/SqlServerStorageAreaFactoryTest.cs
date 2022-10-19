using System.Data.SqlClient;
using DotJEM.Json.Storage2.SqlServer;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Storage2.Test;

public class SqlServerStorageContextIntegrationTest
{
    [Test]
    public async Task EnsureLogTable_NoTableExists_ShouldCreateTable()
    {
        SqlServerStorageContext context = await SqlServerStorageContext.Create(TestSqlConnectionFactory.ConnectionString);
        IStorageArea area = await context.AreaAsync("test");
        await area.InsertAsync("na", new JObject());
    }

    [Test]
    public async Task EnsureLogTable_NoTableExists_ShouldCreateTables()
    {
        SqlServerStorageContext context = await SqlServerStorageContext.Create(TestSqlConnectionFactory.ConnectionString, "fox");
        IStorageArea area = await context.AreaAsync("test");
        StorageObject so = await area.InsertAsync("na",new JObject());

        Console.WriteLine(so);

        StorageObject? so2 = await area.GetAsync(so.Id);
        Console.WriteLine(so2);
    }
}