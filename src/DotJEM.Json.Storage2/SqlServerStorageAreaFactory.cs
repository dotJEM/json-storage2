using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;

namespace DotJEM.Json.Storage2;

public interface IAreaInformationCollection
{
    Task<bool> ExistsAsync(string name);
}

public class SqlServerAreaInformationCollection : IAreaInformationCollection
{
    private bool initialized;
    private readonly SqlServerStorageContext context;

    public SqlServerAreaInformationCollection(SqlServerStorageContext context)
    {
        this.context = context;
    }

    public async Task<bool> ExistsAsync(string name)
    {
        if (!initialized)
        {
            await this.Initialize();
        }


        return true;
    }

    private async Task Initialize()
    {
        await using SqlConnection connection = context.CreateConnection();
        await connection.OpenAsync();
        HashSet<string> schemas = await LoadSchemas(connection);


    }

    private async Task<HashSet<string>> LoadSchemas(SqlConnection connection)
    {
        await using SqlCommand command = new (SqlServerStatements.Load("SelectSchemaNames"));
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
        this.areas = new SqlServerAreaInformationCollection(context);
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

        CreateAreaCommand command = new (connection, new CreateAreaCommand.Statements(DB_SCHEMA, name));
        await command.ExecuteAsync();

        return new SqlServerStorageArea(context, name);
    }

}



public class CreateAreaCommand
{
    public record Statements
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

    public CreateAreaCommand(SqlConnection connection, Statements statements)
    {
        this.connection = connection;
        this.statements = statements;
    }
    
    public async Task ExecuteAsync()
    {
        SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted).ConfigureAwait(false);
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