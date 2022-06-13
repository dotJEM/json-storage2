using System.Data.SqlClient;

namespace DotJEM.Json.Storage2.Test;

public static class TestSqlConnectionFactory
{
    public static string ConnectionString =
        Environment.GetEnvironmentVariable("mssql_connection") ??
        "Data Source=.\\DEV;Initial Catalog=json-storage2-test;Integrated Security=True";



    public static SqlConnection CreateConnection() => new (ConnectionString);
}