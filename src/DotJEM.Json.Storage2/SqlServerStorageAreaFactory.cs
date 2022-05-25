using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;

namespace DotJEM.Json.Storage2;

public interface IAreaInformationCollection
{
    
}

public class SqlServerStorageAreaFactory
{
    private const string DB_SCHEMA = "dbo";

    private readonly SqlServerStorageContext context;
    private readonly IAreaInformationCollection areas;

    public SqlServerStorageAreaFactory(SqlServerStorageContext context)
    {
        this.context = context;
    }

    public async Task<SqlServerStorageArea> Create(string name)
    {
        await using SqlConnection connection = context.CreateConnection();
        await using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);

        await connection.OpenAsync().ConfigureAwait(false);

        CreateAreaCommand command = new (connection, new CreateAreaCommandStatements(DB_SCHEMA, name));
        await command.ExecuteAsync();

        return new SqlServerStorageArea(context, name);
    }

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

public record CreateAreaCommandStatements
{
    public string DataTable { get; }
    public string LogTable { get; }
    public string SchemaTable { get; }

    public CreateAreaCommandStatements(string schema, string name)
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

public class CreateAreaCommand
{
    private readonly SqlConnection connection;
    private readonly CreateAreaCommandStatements statements;

    public CreateAreaCommand(SqlConnection connection, CreateAreaCommandStatements statements)
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