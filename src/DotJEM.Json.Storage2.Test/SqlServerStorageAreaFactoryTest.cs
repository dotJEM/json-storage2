using System.Data.SqlClient;
using DotJEM.Json.Storage2.SqlServer;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Storage2.Test;

[TestFixture]
public class CreateAreaCommandTest
{
    [Test]
    public async Task EnsureLogTable_NoTableExists_ShouldCreateTable()
    {
        SqlConnection connection = TestSqlConnectionFactory.CreateConnection();
        await connection.OpenAsync();
        SqlTransaction transaction = connection.BeginTransaction();

        CreateAreaCommand command = new (connection, "dbo", "myTable");
        await command.ExecuteAsync(transaction);



    }
}

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
        await area.InsertAsync("na",new JObject());
    }
}