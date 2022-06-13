using System.Data.SqlClient;
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
        
        CreateAreaCommand command = new (connection, new CreateAreaCommand.Statements("dbo", "myTable"));
        await command.ExecuteAsync();



    }
}