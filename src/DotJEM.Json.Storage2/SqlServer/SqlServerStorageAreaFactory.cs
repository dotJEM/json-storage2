using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;

namespace DotJEM.Json.Storage2.SqlServer;

public interface IAreaInformationCollection
{
    void Add(string name);
    Task<bool> ExistsAsync(string name);
    Task<bool> SchemaExists(string dbSchema);
}

public readonly record struct AreaInfo(string Name, string DataTableName, string LogTableName, string SchemasTableName);

public class SqlServerAreaInformationCollection : IAreaInformationCollection
{
    private bool initialized = false;
    private readonly string schema;
    private readonly SqlServerStorageContext context;

    private bool schemaExists = false;
    private readonly ConcurrentDictionary<string, AreaInfo> areas = new();
    private readonly SemaphoreSlim padlock = new (1, 1);

    public SqlServerAreaInformationCollection(SqlServerStorageContext context, string schema)
    {
        this.context = context;
        this.schema = schema;
    }

    public void Add(string name)
    {
        areas.TryAdd(name, new AreaInfo(name, $"{name}.data", $"{name}.log", $"{name}.schemas"));
    }

    public async Task<bool> SchemaExists(string dbSchema)
    {
        if (!initialized)
        {
            await padlock.WaitAsync();
            try
            {
                if (!initialized)
                {
                    await Initialize();
                }
            }
            finally
            {
                padlock.Release();
            }
        }
        return schemaExists;
    }

    public async Task<bool> ExistsAsync(string name)
    {
        if (!initialized)
        {
            await padlock.WaitAsync();
            try
            {
                if (!initialized)
                {
                    await Initialize();
                }
            }
            finally
            {
                padlock.Release();
            }
        }
        return areas.TryGetValue(name, out _);
    }

    private async Task Initialize()
    {
        await using SqlConnection connection = context.CreateConnection();
        await connection.OpenAsync();
        HashSet<string> schemas = await LoadSchemas(connection);
        if (!schemas.Contains(schema))
            return;

        schemaExists = true;
        await LoadAreas(connection);
        initialized = true;
    }

    private async Task LoadAreas(SqlConnection connection)
    {
        await using SqlCommand command = new(SqlServerStatements.Load("SelectTableNames"));
        command.Connection = connection;

        await using SqlDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        Dictionary<string, AreaInfo> areas = new();
        while (await reader.ReadAsync())
        {
            //string catalog = reader.GetString(0);
            //string schema = reader.GetString(1);
            string tableName = reader.GetString(2);
            string area = tableName.Substring(0, tableName.LastIndexOf('.'));
            if (areas.ContainsKey(area))
                continue;

            this.Add(area);
        }
    }

    private async Task<HashSet<string>> LoadSchemas(SqlConnection connection)
    {
        await using SqlCommand command = new(SqlServerStatements.Load("SelectSchemaNames"));
        command.Connection = connection;
        await using SqlDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

        HashSet<string> names = new();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));
        return names;
    }

    //public async Task<IAreaInformationCollection> InitializeAsync(SqlConnection connection)
    //{
    //    string selectSchemasCommandText = SqlServerStatements.Load("SelectSchemaNames");
    //    await using SqlCommand command = new SqlCommand(selectSchemasCommandText);

    //    await using SqlDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);



    //    string selectTablesCommandText = SqlServerStatements.Load("SelectTableNames");


    //    return null;
    //}

    //private async Task<IAreaInformationCollection> LoadAreaInformation()
    //{
    //    string commandText = Resources.Load("Systematic.Odiss.Server.Data.SqlServer.Statements.SelectTableNames.sql");

    //    await using SqlConnection conn = context.CreateConnection();
    //    await using SqlCommand command = new(commandText);
    //    command.Connection = conn;

    //    await conn.OpenAsync().ConfigureAwait(false);
    //    SqlDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);



    //}
}

public class SqlServerStorageAreaFactory
{
    private const string DB_SCHEMA = "data";

    private readonly SqlServerStorageContext context;
    private readonly IAreaInformationCollection areas;

    public SqlServerStorageAreaFactory(SqlServerStorageContext context)
    {
        this.context = context;
        areas = new SqlServerAreaInformationCollection(context, DB_SCHEMA);
    }

    public async Task<SqlServerStorageArea> Create(string name)
    {
        if (await areas.ExistsAsync(name))
        {
            return new SqlServerStorageArea(context, name);
        }
        await using SqlConnection connection = context.CreateConnection();
        await using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);
        await connection.OpenAsync().ConfigureAwait(false);

        IDbCommandList command = (await areas.SchemaExists(DB_SCHEMA))
            ? new CreateAreaCommand(connection, DB_SCHEMA, name)
            : new CreateAreaAndSchemaCommand(connection, DB_SCHEMA, name);
        await command.ExecuteAsync(transaction).ConfigureAwait(false);
        await transaction.CommitAsync().ConfigureAwait(false);
        areas.Add(name);
        return new SqlServerStorageArea(context, name);
    }

}

public interface IDbCommandList
{
    Task ExecuteAsync(SqlTransaction transaction);
}

public class CreateAreaAndSchemaCommand : IDbCommandList
{
    private record Statements
    {
        public string CreateSchema { get; }

        public Statements(string schema)
        {
            Dictionary<string, string> map = new() {
                { "schema", schema }
            };
            CreateSchema = SqlServerStatements.Load("CreateSchema", map);
        }
    }


    private readonly CreateAreaCommand createAreaCommand;
    private readonly SqlConnection connection;
    private readonly Statements statements;

    public CreateAreaAndSchemaCommand(SqlConnection connection, string schema, string name)
    {
        this.createAreaCommand = new CreateAreaCommand(connection, schema, name);
        this.statements = new Statements(name);
        this.connection = connection;
    }

    public async Task ExecuteAsync(SqlTransaction transaction)
    {
        //SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted).ConfigureAwait(false);
        await CreateSchema(transaction).ConfigureAwait(false);
        await createAreaCommand.ExecuteAsync(transaction).ConfigureAwait(false);
    }
    public async Task CreateSchema(SqlTransaction transaction)
    {
        await using SqlCommand command = new(statements.CreateSchema);
        command.Connection = connection;
        command.Transaction = transaction;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}

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