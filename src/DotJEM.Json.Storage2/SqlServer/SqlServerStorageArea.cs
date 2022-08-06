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

        throw new NotImplementedException();
    }

    public Task<StorageObject> InsertAsync(string contentType, JObject obj)
    {
        return InsertAsync(new StorageObject(contentType, Guid.Empty, 0, DateTime.MinValue, DateTime.MinValue, obj));
    }

    public async Task<StorageObject> InsertAsync(StorageObject obj)
    {
        await stateManager.Ensure();

        string commandText = SqlServerStatements.Load("InsertIntoDataTable", "normal", ("schema", stateManager.Schema), ("data_table_name", $"{stateManager.AreaName}.data"));

        await using SqlConnection connection = context.ConnectionFactory.Create();
        await using SqlCommand command = new SqlCommand(commandText, connection);

        await connection.OpenAsync().ConfigureAwait(false);
        await using SqlTransaction transaction = connection.BeginTransaction();
        command.Transaction = transaction;
        
        command.Parameters.Add("@contentType", SqlDbType.NVarChar).Value = obj.ConcentType;
        command.Parameters.Add("@timestamp", SqlDbType.DateTime).Value = DateTime.UtcNow;
        command.Parameters.Add("@data", SqlDbType.NVarChar).Value = obj.Data.ToString(Formatting.None);

        await command.ExecuteNonQueryAsync().ConfigureAwait(false);

        await transaction.CommitAsync().ConfigureAwait(false);

        //TODO: Fill with outputs.
        return obj;
    }



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