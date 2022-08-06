using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;

namespace DotJEM.Json.Storage2.SqlServer;


//public static class ManagerFactory
//{
//    public static async Task<SqlServerAreasInformationManager> Create(string schema, SqlServerStorageContext context)
//    {
//        await using SqlConnection connection = context.CreateConnection();
//        await connection.OpenAsync();

//        await using SqlCommand command = new(SqlServerStatements.Load("SelectSchemaExists"));
//        command.Parameters.Add("schema", SqlDbType.NVarChar).Value = schema;

//        int schemaExists = (int)(await command.ExecuteScalarAsync().ConfigureAwait(false) ?? throw new Exception());

//    }
//}

//public interface ISchemasInformationManager
//{
//    Task<IAreasInformationManager> GetOrCreate(string schema);
//}

//public class SqlServerSchemasInformationManager : ISchemasInformationManager
//{
//    public static async Task<SqlServerSchemasInformationManager> Create(SqlServerStorageContext context)
//    {
//        await using SqlConnection connection = context.CreateConnection();
//        await connection.OpenAsync();
//        await using SqlCommand command = new(SqlServerStatements.Load("SelectSchemaNames"));
//        command.Connection = connection;
//        await using SqlDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

//        HashSet<string> names = new();
//        while (await reader.ReadAsync())
//            names.Add(reader.GetString(0));

//        return new SqlServerSchemasInformationManager(context, names.ToDictionary(name => name, name => SqlServerAreasInformationManager.Create(name, context)));
//    }

//    private readonly SqlServerStorageContext context;
//    private readonly Dictionary<string, SqlServerAreasInformationManager> schemas;

//    private SqlServerSchemasInformationManager(SqlServerStorageContext context, Dictionary<string, SqlServerAreasInformationManager> schemas)
//    {
//        this.context = context;
//        this.schemas = schemas;
//    }

//    public async Task<IAreasInformationManager> GetOrCreate(string schema)
//    {
//        if (this.schemas.ContainsKey(schema))
//            return this.schemas[schema];

//        string commandText = SqlServerStatements.Load("CreateSchema", ("schema", schema));

//        await using SqlConnection connection = context.CreateConnection();
//        await using SqlCommand command = new SqlCommand(commandText, connection);
//        await connection.OpenAsync().ConfigureAwait(false);

//        await using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);
//        command.Connection = connection;
//        command.Transaction = transaction;
//        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
//        await transaction.CommitAsync().ConfigureAwait(false);

//        SqlServerAreasInformationManager manager =  new SqlServerAreasInformationManager(context, schema);
//        this.schemas.Add(schema, manager);
//        return manager;
//    }
//}

//public interface IAreasInformationManager
//{
//    Task<IAreaInformation> GetOrCreate(string area);
//}

//public class SqlServerAreasInformationManager : IAreasInformationManager
//{
//    public static async Task<SqlServerAreasInformationManager> Create(string schema, SqlServerStorageContext context)
//    {
//        await using SqlConnection connection = context.CreateConnection();
//        await connection.OpenAsync();
//        await using SqlCommand command = new(SqlServerStatements.Load("SelectTableNames"));
//        command.Connection = connection;
//        command.Parameters.Add(new SqlParameter("schema", SqlDbType.NVarChar)).Value = schema;
        
//        await using SqlDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

//        HashSet<string> names = new();
//        while (await reader.ReadAsync())
//        {
//            string tableName = reader.GetString(2);
//            string area = tableName.Substring(0, tableName.LastIndexOf('.'));
//            names.Add(area);
//        }
//        return new SqlServerAreasInformationManager(context, schema, names.ToDictionary(name => name, name => new AreaInfo(name, $"{name}.data", $"{name}.log", $"{name}.schemas")));
//    }

//    private readonly string schema;
//    private readonly SqlServerStorageContext context;
//    private readonly Dictionary<string, AreaInfo> areas;

//    public SqlServerAreasInformationManager(SqlServerStorageContext context, string schema, Dictionary<string, AreaInfo> areas)
//    {
//        this.context = context;
//        this.schema = schema;
//        this.areas = areas;
//    }

//    public Task<IAreaInformation> GetOrCreate(string area)
//    {
//        throw new NotImplementedException();
//    }
//}

//public interface IAreaInformation
//{
//}

//public interface IAreaInformationCollection
//{
//    void Add(string name);
//    Task<bool> ExistsAsync(string name);
//    Task<bool> SchemaExists(string dbSchema);
//}

//public interface ISchemasInformationCollection
//{
//    void Add(string name);
//    Task<bool> ExistsAsync(string name);
//}

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

//public readonly record struct SchemaInfo(string Name);

//public class SqlServerSchemaInformationCollection : ISchemasInformationCollection
//{
//    private bool initialized = false;
//    private readonly SqlServerStorageContext context;
//    private readonly ConcurrentDictionary<string, SchemaInfo> schemas = new();
//    private readonly SemaphoreSlim padlock = new(1, 1);

//    public SqlServerSchemaInformationCollection(SqlServerStorageContext context)
//    {
//        this.context = context;
//    }

//    public void Add(string name)
//    {
//        throw new NotImplementedException();
//    }

//    public async Task<bool> ExistsAsync(string name)
//    {
//        if (initialized)
//            return schemas.TryGetValue(name, out _);

//        await padlock.WaitAsync();
//        try
//        {
//            if (!initialized)
//            {
//                await Initialize();
//            }
//        }
//        finally
//        {
//            padlock.Release();
//        }
//        return schemas.TryGetValue(name, out _);
//    }

//    private async Task Initialize()
//    {
//        await using SqlConnection connection = context.CreateConnection();
//        await connection.OpenAsync();
//        HashSet<string> schemas = await LoadSchemas(connection);
//        foreach (string schema in schemas)
//        {
//            this.schemas.GetOrAdd(schema, new SchemaInfo(schema));
//        }
//    }

//    private async Task<HashSet<string>> LoadSchemas(SqlConnection connection)
//    {
//        await using SqlCommand command = new(SqlServerStatements.Load("SelectSchemaNames"));
//        command.Connection = connection;
//        await using SqlDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

//        HashSet<string> names = new();
//        while (await reader.ReadAsync())
//            names.Add(reader.GetString(0));
//        return names;
//    }
//}

//public class SqlServerAreaInformationCollection : IAreaInformationCollection
//{
//    private bool initialized = false;
//    private readonly string schema;
//    private readonly SqlServerConnectionFactory connectionFactory;

//    private bool schemaExists = false;
//    private readonly ConcurrentDictionary<string, AreaInfo> areas = new();
//    private readonly SemaphoreSlim padlock = new (1, 1);

//    public SqlServerAreaInformationCollection(SqlServerConnectionFactory connectionFactory, string schema)
//    {
//        this.connectionFactory = connectionFactory;
//        this.schema = schema;
//    }

//    public void Add(string name)
//    {
//        areas.TryAdd(name, new AreaInfo(name, $"{name}.data", $"{name}.log", $"{name}.schemas"));
//    }

//    public async Task<bool> SchemaExists(string dbSchema)
//    {
//        if (initialized)
//            return schemaExists;
        
//        await padlock.WaitAsync();
//        try
//        {
//            if (!initialized)
//            {
//                await Initialize();
//            }
//        }
//        finally
//        {
//            padlock.Release();
//        }
//        return schemaExists;
//    }

//    public async Task<bool> ExistsAsync(string name)
//    {
//        if (initialized)
//            return areas.TryGetValue(name, out _);
        
//        await padlock.WaitAsync();
//        try
//        {
//            if (!initialized)
//            {
//                await Initialize();
//            }
//        }
//        finally
//        {
//            padlock.Release();
//        }
//        return areas.TryGetValue(name, out _);
//    }

//    private async Task Initialize()
//    {
//        await using SqlConnection connection = (SqlConnection)connectionFactory.Create();
//        await connection.OpenAsync();
//        await LoadAreas(connection);
//        initialized = true;
//    }

//    private async Task LoadAreas(SqlConnection connection)
//    {
//        await using SqlCommand command = new(SqlServerStatements.Load("SelectTableNames"));
//        command.Connection = connection;

//        await using SqlDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
//        Dictionary<string, AreaInfo> areas = new();
//        while (await reader.ReadAsync())
//        {
//            //string catalog = reader.GetString(0);
//            //string schema = reader.GetString(1);
//            string tableName = reader.GetString(2);
//            string area = tableName.Substring(0, tableName.LastIndexOf('.'));
//            if (areas.ContainsKey(area))
//                continue;

//            this.Add(area);
//        }
//    }

//}

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
    private readonly SemaphoreSlim padlock = new(1, 1);

    public string Schema { get; }
    public string AreaName { get; }
    public bool Exists => created;

    public SqlServerAreaStateManager(ISqlServerConnectionFactory connectionFactory, string schema, string area, bool created)
    {
        this.connectionFactory = connectionFactory;
        this.Schema = schema;
        this.AreaName = area;
        this.created = created;
    }

    public async ValueTask Ensure()
    {
        if (created)
            return;

        await padlock.WaitAsync();

        Dictionary<string, string> map = new() {
            { "schema", Schema },
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