using System;
using System.Threading;
using System.Threading.Tasks;
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


#if NETSTANDARD2_0

#else
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
#endif



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

    public Task<StorageObject> InsertAsync(string contentType, JObject obj)
        => InsertAsync(new StorageObject(contentType, Guid.Empty, 0, DateTime.MinValue, DateTime.MinValue, obj));

    public async Task<StorageObject> InsertAsync(StorageObject obj)
    {
        await stateManager.Ensure();
        DateTime timeStamp = DateTime.UtcNow; ;
        using ISqlServerCommand cmd = context.CommandBuilder
            .From("InsertIntoDataTable")
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
        DateTime timeStamp = DateTime.UtcNow; ;
        using ISqlServerCommand cmd = context.CommandBuilder
            .From("UpdateDataTable")
            .Replace(
                ("schema", stateManager.Schema),
                ("data_table_name", $"{stateManager.AreaName}.data"),
                ("log_table_name", $"{stateManager.AreaName}.log")
            )
            .Parameters(
                ("id", obj.Id),
                ("timestamp", timeStamp),
                ("data", obj.Data.ToString(Formatting.None))
            )
            .Build();

        using ISqlServerDataReader<StorageObject> read = await cmd
            .ExecuteReaderAsync(
                new[] { "ContentType", "Version", "Created" },
                values => obj with { ContentType = (string)values[0], Version = (int)values[1], Created = (DateTime)values[2] },
                CancellationToken.None);


        return read.FirstOrDefault(); 
    }

    public Task<StorageObject> UpdateAsync(Guid id, JObject obj)
        => UpdateAsync(new StorageObject(string.Empty, id, -1, DateTime.MinValue, DateTime.MinValue, obj));

    public Task<StorageObject?> DeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }


}