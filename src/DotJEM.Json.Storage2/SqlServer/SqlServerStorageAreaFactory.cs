using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using DotJEM.Json.Storage2.SqlServer.Initialization;

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
    private readonly Dictionary<string, AreaInfo> areas;
    private readonly SemaphoreSlim padlock = new(1, 1);

    public SqlServerStorageAreaFactory(ISqlServerSchemaStateManager schemaState, Dictionary<string, AreaInfo> areas = null)
    {
        this.schema = schemaState;
        this.areas = areas ?? new Dictionary<string, AreaInfo>();
    }

    public async Task<SqlServerStorageArea> Create(string name, SqlServerStorageContext context)
    {
        await schema.Ensure();
        if (areas.TryGetValue(name, out AreaInfo? areaInfo))
            return new SqlServerStorageArea(context, areaInfo.State);

        await padlock.WaitAsync().ConfigureAwait(false);
        AreaInfo area = new AreaInfo(name, new SqlServerAreaStateManager(context.ConnectionFactory, schema.SchemaName, name, false));
        areas.Add(name, area);
        return new SqlServerStorageArea(context, area.State);
    }
}
