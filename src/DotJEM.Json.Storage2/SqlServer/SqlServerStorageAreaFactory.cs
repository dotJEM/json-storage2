using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;

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

public static class Temp
{
    public static async Task<SqlServerStorageAreaFactory> Create(string schema, SqlServerConnectionFactory connectionFactory)
    {
        //TODO: Eliminate cast?
        await using SqlConnection connection = (SqlConnection)connectionFactory.Create();
        await connection.OpenAsync().ConfigureAwait(false);

        await using SqlCommand command = new(SqlServerStatements.Load("SelectSchemaExists"));
        command.Parameters.Add("schema", SqlDbType.NVarChar).Value = schema;
        command.Connection = connection;

        int schemaExists = (int)(await command.ExecuteScalarAsync().ConfigureAwait(false) ?? throw new Exception());
        if (schemaExists == 0)
        {
            //TODO: Needs to pass a state object to track creation of schema.
            return new SqlServerStorageAreaFactory(new SqlServerSchemaStateManager(connectionFactory, schema, false));
        }

        await using SqlCommand command2 = new(SqlServerStatements.Load("SelectTableNames"));
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
                new AreaInfo(name,  new SqlServerAreaStateManager(connectionFactory, schema,name,true)));

        return new SqlServerStorageAreaFactory(new SqlServerSchemaStateManager(connectionFactory, schema, true), areas);
    }
}

public interface ISqlServerSchemaStateManager
{
    string SchemaName { get; }

    ValueTask Ensure();
}

public class SqlServerSchemaStateManager : ISqlServerSchemaStateManager
{
    private bool created;
    private readonly SqlServerConnectionFactory connectionFactory;
    private readonly SemaphoreSlim padlock = new(1, 1);
    public string SchemaName { get; }

    public SqlServerSchemaStateManager(SqlServerConnectionFactory connectionFactory, string schema, bool created)
    {
        this.connectionFactory = connectionFactory;
        this.SchemaName = schema;
        this.created = created;
    }

    public async ValueTask Ensure()
    {
        if(created)
            return;

        await padlock.WaitAsync();

        string commandText = SqlServerStatements.Load("CreateSchema", ("schema", SchemaName));

        await using SqlConnection connection = connectionFactory.Create();
        await using SqlCommand command = new SqlCommand(commandText, connection);
        await connection.OpenAsync().ConfigureAwait(false);

        await using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);
        command.Connection = connection;
        command.Transaction = transaction;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        await transaction.CommitAsync().ConfigureAwait(false);
        created = true;

        padlock.Release();
    }
}


public class SqlServerAreaStateManager
{
    private bool created;
    private readonly ISqlServerConnectionFactory connectionFactory;
    private readonly string schema;
    private readonly SemaphoreSlim padlock = new(1, 1);
    public string AreaName { get; }
    public bool Exists => created;

    public SqlServerAreaStateManager(ISqlServerConnectionFactory connectionFactory, string schema, string area, bool created)
    {
        this.connectionFactory = connectionFactory;
        this.schema = schema;
        this.AreaName = area;
        this.created = created;
    }

    public async ValueTask Ensure()
    {
        if (created)
            return;

        await padlock.WaitAsync();

        Dictionary<string, string> map = new() {
            { "schema", schema },
            { "data_table_name", $"{AreaName}.data" },
            { "log_table_name", $"{AreaName}.log" },
            { "schema_table_name", $"{AreaName}.schemas" }
        };
        string dataTableCommandText = SqlServerStatements.Load("CreateDataTable", map);
        string logTableCommandText = SqlServerStatements.Load("CreateLogTable", map);
        string schemaTableCommandText = SqlServerStatements.Load("CreateSchemasTable", map);

        await using SqlConnection connection = connectionFactory.Create();
        await connection.OpenAsync().ConfigureAwait(false);

        await using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);
        await Execute(dataTableCommandText, connection, transaction);
        await Execute(logTableCommandText, connection, transaction);
        await Execute(schemaTableCommandText, connection, transaction);
        await transaction.CommitAsync().ConfigureAwait(false);
        created = true;
        padlock.Release();
    }

    private async Task Execute(string commandText, SqlConnection connection, SqlTransaction transaction)
    {
        await using SqlCommand command = new(commandText);
        command.Connection = connection;
        command.Transaction = transaction;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
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

public interface IDbCommandList
{
    Task ExecuteAsync(SqlTransaction transaction);
}

//public class CreateAreaAndSchemaCommand : IDbCommandList
//{
//    private record Statements
//    {
//        public string CreateSchema { get; }

//        public Statements(string schema)
//        {
//            Dictionary<string, string> map = new() {
//                { "schema", schema }
//            };
//            CreateSchema = SqlServerStatements.Load("CreateSchema", map);
//        }
//    }


//    private readonly CreateAreaCommand createAreaCommand;
//    private readonly SqlConnection connection;
//    private readonly Statements statements;

//    public CreateAreaAndSchemaCommand(SqlConnection connection, string schema, string name)
//    {
//        this.createAreaCommand = new CreateAreaCommand(connection, schema, name);
//        this.statements = new Statements(name);
//        this.connection = connection;
//    }

//    public async Task ExecuteAsync(SqlTransaction transaction)
//    {
//        //SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted).ConfigureAwait(false);
//        await CreateSchema(transaction).ConfigureAwait(false);
//        await createAreaCommand.ExecuteAsync(transaction).ConfigureAwait(false);
//    }
//    public async Task CreateSchema(SqlTransaction transaction)
//    {
//        await using SqlCommand command = new(statements.CreateSchema);
//        command.Connection = connection;
//        command.Transaction = transaction;
//        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
//    }
//}

public class CreateAreaCommand : IDbCommandList
{
    private record Statements
    {
        public string DataTable { get; }
        public string LogTable { get; }
        public string SchemaTable { get; }

        public Statements(string schema, string name)
        {
            Dictionary<string, string> map = new() {
                { "schema", schema },
                { "data_table_name", $"{name}.data" },
                { "log_table_name", $"{name}.log" },
                { "schema_table_name", $"{name}.schemas" }
            };
            DataTable = SqlServerStatements.Load("CreateDataTable", map);
            LogTable = SqlServerStatements.Load("CreateLogTable", map);
            SchemaTable = SqlServerStatements.Load("CreateSchemasTable", map);
        }
    }

    private readonly SqlConnection connection;
    private readonly Statements statements;

    public CreateAreaCommand(SqlConnection connection, string schema, string name)
    {
        this.connection = connection;
        this.statements = new Statements(schema, name);
    }

    public async Task ExecuteAsync(SqlTransaction transaction)
    {
        //SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted).ConfigureAwait(false);
        await CreateDataTableAsync(transaction).ConfigureAwait(false);
        await CreateLogTableAsync(transaction).ConfigureAwait(false);
        await CreateSchemaTableAsync(transaction).ConfigureAwait(false);
        await transaction.CommitAsync().ConfigureAwait(false);
    }

    public async Task CreateDataTableAsync(SqlTransaction transaction)
    {
        await using SqlCommand command = new(statements.DataTable);
        command.Connection = connection;
        command.Transaction = transaction;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async ValueTask CreateLogTableAsync(SqlTransaction transaction)
    {
        await using SqlCommand command = new(statements.LogTable);
        command.Connection = connection;
        command.Transaction = transaction;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async Task CreateSchemaTableAsync(SqlTransaction transaction)
    {
        await using SqlCommand command = new(statements.SchemaTable);
        command.Connection = connection;
        command.Transaction = transaction;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

}