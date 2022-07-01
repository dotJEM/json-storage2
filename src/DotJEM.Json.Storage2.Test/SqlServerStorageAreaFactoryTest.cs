using System.Data.SqlClient;
using DotJEM.Json.Storage2.SqlServer;
using NUnit.Framework;

namespace DotJEM.Json.Storage2.Test;

[TestFixture]
public class CreateAreaCommandTest
{
    [Test]
    public async Task EnsureLogTable_NoTableExists_ShouldCreateTable()
    {
        SqlConnection connection = TestSqlConnectionFactory.CreateConnection();
        SqlTransaction transaction = connection.BeginTransaction();
        await connection.OpenAsync();
        
        CreateAreaCommand command = new (connection, "dbo", "myTable");
        await command.ExecuteAsync(transaction);



    }
}