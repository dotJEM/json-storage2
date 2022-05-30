using System.Data.SqlClient;
using DotJEM.Json.Storage2;
using NUnit.Framework;

namespace Systematic.Odiss.Server.Data.Test;

[TestFixture]
public class CreateAreaCommandTest
{
    [Test]
    public async Task EnsureLogTable_NoTableExists_ShouldCreateTable()
    {
        SqlConnection connection = TestSqlConnectionFactory.CreateConnection();
        await connection.OpenAsync();
        CreateAreaCommand command = new (connection, new CreateAreaCommand.Statements("dbo", "myTable"));
        await command.ExecuteAsync();


    }
}

public static class TestSqlConnectionFactory
{
    public static string ConnectionString =
        Environment.GetEnvironmentVariable("mssql_connection") ??
        "Data Source=.\\DEV;Initial Catalog=json-storage2-test;Integrated Security=True";



    public static SqlConnection CreateConnection() => new (ConnectionString);
}