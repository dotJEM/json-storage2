using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Storage2.SqlServer;

public class SqlServerStorageArea : IStorageArea
{
    private readonly SqlServerStorageContext context;
    private readonly SqlServerAreaStateManager stateManager;

    public string Name { get; }

    public SqlServerStorageArea(SqlServerStorageContext context, SqlServerAreaStateManager stateManager)
    {
        this.context = context;
        this.stateManager = stateManager;
    }

    public async IAsyncEnumerable<StorageObject> GetAsync()
    {
        if (!stateManager.Exists)
            yield break;




        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<StorageObject> GetAsync(long skip, int take = 100)
    {
        if (!stateManager.Exists)
            yield break;

        throw new NotImplementedException();
    }

    public async Task<StorageObject?> GetAsync(Guid id)
    {
        if (!stateManager.Exists)
            return null;

        string commandText = SqlServerStatements.Load("SelectFromDataTable", "byid",
            ("schema", stateManager.Schema),
            ("data_table_name", $"{stateManager.AreaName}.data"));

        await using SqlConnection connection = context.ConnectionFactory.Create();
        await using SqlCommand command = new SqlCommand(commandText, connection);
        await connection.OpenAsync().ConfigureAwait(false);
        command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = id;

        await using SqlDataReader? reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        return RunDataReader(reader).FirstOrDefault();
    }


    private IEnumerable<StorageObject> RunDataReader(SqlDataReader reader)
    {
        int idColumn = reader.GetOrdinal(nameof(StorageObject.Id));
        int contentTypeColumn = reader.GetOrdinal(nameof(StorageObject.ContentType));
        int versionColumn = reader.GetOrdinal(nameof(StorageObject.Version));
        int createdColumn = reader.GetOrdinal(nameof(StorageObject.Created));
        int updatedColumn = reader.GetOrdinal(nameof(StorageObject.Updated));
        int dataColumn = reader.GetOrdinal(nameof(StorageObject.Data));
        while (reader.Read())
        {
            JObject json = JObject.Parse(reader.GetString(dataColumn));
            yield return new StorageObject(
                reader.GetString(contentTypeColumn),
                reader.GetGuid(idColumn),
                reader.GetInt32(versionColumn),
                reader.GetDateTime(createdColumn),
                reader.GetDateTime(updatedColumn),
                json
            );
        }
    }

    public Task<StorageObject> InsertAsync(string contentType, JObject obj)
    {
        return InsertAsync(new StorageObject(contentType, Guid.Empty, 0, DateTime.MinValue, DateTime.MinValue, obj));
    }

    public async Task<StorageObject> InsertAsync(StorageObject obj)
    {
        await stateManager.Ensure();

        string commandText = SqlServerStatements.Load("InsertIntoDataTable", "normal", 
            ("schema", stateManager.Schema),
            ("data_table_name", $"{stateManager.AreaName}.data"),
            ("log_table_name", $"{stateManager.AreaName}.log"));

        await using SqlConnection connection = context.ConnectionFactory.Create();
        await using SqlCommand command = new SqlCommand(commandText, connection);

        await connection.OpenAsync().ConfigureAwait(false);
        await using SqlTransaction transaction = connection.BeginTransaction();
        command.Transaction = transaction;
        
        DateTime timeStamp = DateTime.UtcNow;;
        command.Parameters.Add("@contentType", SqlDbType.NVarChar).Value = obj.ContentType;
        command.Parameters.Add("@timestamp", SqlDbType.DateTime).Value = timeStamp;
        command.Parameters.Add("@data", SqlDbType.NVarChar).Value = obj.Data.ToString(Formatting.None);

        Guid id = (Guid)(await command.ExecuteScalarAsync().ConfigureAwait(false) ?? throw new InvalidOperationException());

        await transaction.CommitAsync().ConfigureAwait(false);

        return obj with { Id = id, Created = timeStamp, Updated = timeStamp, Version = 0 };
    }

    // This is very back and forth, but maybe switching back to binary would be better for storage and retrieval speeds. In the end, maybe this should be a 
    // external choice.
    // https://learn.microsoft.com/da-dk/archive/blogs/sqlserverstorageengine/storing-json-in-sql-server#compressed-json-storage

    public Task<StorageObject> UpdateAsync(Guid id, StorageObject obj)
    {
        throw new NotImplementedException();
    }

    public Task<StorageObject> UpdateAsync(Guid id, JObject obj)
    {
        throw new NotImplementedException();
    }

    public Task<StorageObject?> DeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }


}