using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotJEM.Json.Storage2.SqlServer.Initialization;

//NOTE: Temp holds "in development" items, these should go into meaningful services under interfaces so they can be substituded e.g. for testing purposes.
public static class Temp
{
    public static async Task<SqlServerStorageAreaFactory> Create(string schema, SqlServerConnectionFactory connectionFactory)
    {
        await using SqlConnection connection = connectionFactory.Create();
        await connection.OpenAsync().ConfigureAwait(false);

        await using SqlCommand command = new(SqlTemplates.SelectSchemaExists());
        command.Parameters.Add("schema", SqlDbType.NVarChar).Value = schema;
        command.Connection = connection;

        //TODO: Throw better exception.
        int schemaExists = (int)(await command.ExecuteScalarAsync().ConfigureAwait(false) ?? throw new Exception());
        if (schemaExists == 0)
        {
            //TODO: Needs to pass a state object to track creation of schema.
            return new SqlServerStorageAreaFactory(new SqlServerSchemaStateManager(connectionFactory, schema, false));
        }

        await using SqlCommand command2 = new(SqlTemplates.SelectTableNames());
        command2.Parameters.Add(new SqlParameter("schema", SqlDbType.NVarChar)).Value = schema;
        command2.Connection = connection;

        await using SqlDataReader reader = await command2.ExecuteReaderAsync().ConfigureAwait(false);


        HashSet<string> names = new();
        while (await reader.ReadAsync())
        {
            string tableName = reader.GetString(0);
            string area = tableName.Substring(0, tableName.LastIndexOf('.'));
            names.Add(area);
        }

        Dictionary<string, AreaInfo> areas = names
            .ToDictionary(name => name, name =>
                new AreaInfo(name, new SqlServerAreaStateManager(connectionFactory, schema, name, true)));

        return new SqlServerStorageAreaFactory(new SqlServerSchemaStateManager(connectionFactory, schema, true), areas);
    }


}

