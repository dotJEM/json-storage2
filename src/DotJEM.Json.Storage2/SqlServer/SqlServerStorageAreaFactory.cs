using Dapper;
using DotJEM.Json.Storage2.SqlServer.Initialization;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace DotJEM.Json.Storage2.SqlServer;

public class AreaInfo
{
    public string Name { get; }
    public string DataTableName { get; }
    public string LogTableName { get; }
    public string SchemasTableName { get; }
    public SqlServerAreaStateManager State { get; }

    public AreaInfo(string name, SqlServerAreaStateManager state)
    {
        Name = name;
        DataTableName = $"{name}.data";
        LogTableName = $"{name}.log";
        SchemasTableName = $"{name}.schemas";
        State = state;
    }
}

public class SqlServerStorageAreaFactory
{
    private readonly ISqlServerSchemaStateManager schema;
    private readonly ISqlServerConnectionFactory connectionFactory;
    private readonly Dictionary<string, AreaInfo> areas;
    private readonly SemaphoreSlim padlock = new(1, 1);

    public SqlServerStorageAreaFactory(ISqlServerSchemaStateManager schemaState, ISqlServerConnectionFactory connectionFactory, Dictionary<string, AreaInfo>? areas = null)
    {
        this.schema = schemaState;
        this.connectionFactory = connectionFactory;
        this.areas = areas ?? new Dictionary<string, AreaInfo>();
    }

    public async Task<SqlServerStorageArea<TJson>> Create<TJson>(string name, SqlServerStorageContext<TJson> context)
    {
        await schema.Ensure();
        if (areas.TryGetValue(name, out AreaInfo? areaInfo))
            return new(context, areaInfo.State);

        await padlock.WaitAsync().ConfigureAwait(false);

        AreaInfo area = new(name, new(context.ConnectionFactory, schema.Schema, name, await ExistsInDatabase<TJson>(name)));
        areas.Add(name, area);
        padlock.Release();
        return new(context, area.State);
    }

    private async Task<bool> ExistsInDatabase<TJson>(string name)
    {
        string tableExists = SqlTemplates.DataTableExists(schema.Schema, name);
        using SqlConnection connection = connectionFactory.Create();
        await connection.OpenAsync().ConfigureAwait(false);
        var result = await connection.ExecuteScalarAsync<int>(tableExists).ConfigureAwait(false);
        return result == 1;
    }
}
