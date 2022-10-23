using System.Data;
using System.Data.SqlClient;
using DotJEM.Json.Storage2.SqlServer.Initialization;
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

        using ISqlServerCommand cmd = context.CommandBuilder
            .From("SelectFromDataTable", "byid")
            .Replace(
                ("schema", stateManager.Schema),
                ("data_table_name", $"{stateManager.AreaName}.data")
            )
            .Parameters(("id", id))
            .Build();

        using ISqlServerDataReader<StorageObject> read = await cmd
            .ExecuteReaderAsync(
                new[] { "Id", "ContentType", "Version", "Created", "Updated", "Data" },
                values => new StorageObject((string)values[1], (Guid)values[0], (int)values[2], (DateTime)values[3], (DateTime)values[4], JObject.Parse((string)values[5])),
                CancellationToken.None);

        return read.FirstOrDefault();
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
        => InsertAsync(new StorageObject(contentType, Guid.Empty, 0, DateTime.MinValue, DateTime.MinValue, obj));

    public async Task<StorageObject> InsertAsync(StorageObject obj)
    {
        await stateManager.Ensure();
        DateTime timeStamp = DateTime.UtcNow; ;
        using ISqlServerCommand cmd = context.CommandBuilder
            .From("InsertIntoDataTable", "normal")
            .Replace(
                ("schema", stateManager.Schema),
                ("data_table_name", $"{stateManager.AreaName}.data"),
                ("log_table_name", $"{stateManager.AreaName}.log")
            )
            .Parameters(
                ("contentType", obj.ContentType),
                ("timestamp", timeStamp),
                ("data", obj.Data.ToString(Formatting.None))
            )
            .Build();
        
        Guid id= await cmd.ExecuteScalarAsync<Guid>(CancellationToken.None).ConfigureAwait(false);
        return obj with { Id = id, Created = timeStamp, Updated = timeStamp, Version = 0 };
    }

    // This is very back and forth, but maybe switching back to binary would be better for storage and retrieval speeds. In the end, maybe this should be a 
    // external choice.
    // https://learn.microsoft.com/da-dk/archive/blogs/sqlserverstorageengine/storing-json-in-sql-server#compressed-json-storage

    public async Task<StorageObject> UpdateAsync(StorageObject obj)
    {
        await stateManager.Ensure();

        string commandText = SqlServerStatements.Load("UpdateDataTable",
            ("schema", stateManager.Schema),
            ("data_table_name", $"{stateManager.AreaName}.data"),
            ("log_table_name", $"{stateManager.AreaName}.log"));

        await using SqlConnection connection = context.ConnectionFactory.Create();
        await using SqlCommand command = new SqlCommand(commandText, connection);
        DateTime timeStamp = DateTime.UtcNow; ;
        command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = obj.Id;
        command.Parameters.Add("@timestamp", SqlDbType.DateTime).Value = timeStamp;
        command.Parameters.Add("@data", SqlDbType.NVarChar).Value = obj.Data.ToString(Formatting.None);

        await connection.OpenAsync().ConfigureAwait(false);
        await using SqlTransaction transaction = connection.BeginTransaction();
        command.Transaction = transaction;
        await using SqlDataReader? reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        StorageObject data = RunDataReaderForUpdate(reader, obj) ?? throw new FileNotFoundException();
        await reader.CloseAsync();
        await transaction.CommitAsync().ConfigureAwait(false);

        return data;
    }

    private  StorageObject? RunDataReaderForUpdate(SqlDataReader reader, StorageObject update)
    {
        int contentTypeColumn = reader.GetOrdinal(nameof(StorageObject.ContentType));
        int versionColumn = reader.GetOrdinal(nameof(StorageObject.Version));
        int createdColumn = reader.GetOrdinal(nameof(StorageObject.Created));
        while (reader.Read())
        {
            return update with { ContentType = reader.GetString(contentTypeColumn), Version = reader.GetInt32(versionColumn), Created = reader.GetDateTime(createdColumn) };
        }

        return null;
    }

    public Task<StorageObject> UpdateAsync(Guid id, JObject obj)
        => UpdateAsync(new StorageObject(string.Empty, id, -1, DateTime.MinValue, DateTime.MinValue, obj));

    public Task<StorageObject?> DeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }


}