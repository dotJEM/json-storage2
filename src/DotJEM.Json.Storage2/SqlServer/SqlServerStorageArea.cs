using System;
using System.Data.SqlTypes;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.Json.Storage2.Generated;
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
    //private const long DEFAULT_SKIP = 0;
    //private const int DEFAULT_TAKE = 100;

    //public async IAsyncEnumerable<StorageObject> GetAsync()
    //    => GetAsync(DEFAULT_SKIP, DEFAULT_TAKE, CancellationToken.None);

    //public async IAsyncEnumerable<StorageObject> GetAsync(CancellationToken cancellation)
    //    => GetAsync(DEFAULT_SKIP, DEFAULT_TAKE, cancellation);

    //public async IAsyncEnumerable<StorageObject> GetAsync(long skip)
    //    => GetAsync(skip, DEFAULT_TAKE, CancellationToken.None);

    //public async IAsyncEnumerable<StorageObject> GetAsync(long skip, CancellationToken cancellation)
    //    => GetAsync(skip, DEFAULT_TAKE, cancellation);

    //public async IAsyncEnumerable<StorageObject> GetAsync(long skip, int take)
    //    => GetAsync(skip, take, CancellationToken.None);

    //public async IAsyncEnumerable<StorageObject> GetAsync(long skip, int take, CancellationToken cancellation)
    //{
    //    if (!stateManager.Exists)
    //        yield break;

    //    throw new NotImplementedException();
    //}
#endif



    public Task<StorageObject?> GetAsync(Guid id) 
        => GetAsync(id, CancellationToken.None);
    
    public async Task<StorageObject?> GetAsync(Guid id, CancellationToken cancellation)
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
                new[] { "Id", "ContentType", "Version", "Created", "Updated", "CreatedBy", "UpdatedBy", "Data" },
                values => new StorageObject(
                    (string)values[1], 
                    (Guid)values[0],
                    (int)values[2],
                    (DateTime)values[3],
                    (DateTime)values[4], 
                    (string)values[5], 
                    (string)values[6], 
                    JObject.Parse((string)values[7])),
                CancellationToken.None);

        return read.FirstOrDefault();
    }

    public Task<StorageObject> InsertAsync(string contentType, JObject obj)
        => InsertAsync(new InsertStorageObject(contentType, obj), CancellationToken.None);
    public Task<StorageObject> InsertAsync(string contentType, JObject obj, CancellationToken cancellation)
        => InsertAsync(new InsertStorageObject(contentType, obj), cancellation);
    public Task<StorageObject> InsertAsync(InsertStorageObject obj)
        => InsertAsync(obj, CancellationToken.None);

    public async Task<StorageObject> InsertAsync(InsertStorageObject obj, CancellationToken cancellation)
    {
        await stateManager.Ensure();

        SqlFiles.CreateDataTable_default("", "");
        

        DateTime timeStamp = obj.Created ?? DateTime.UtcNow;
        string userName = obj.CreatedBy ?? context.UserInformation.UserName;

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
                ("user", userName),
                ("data", obj.Data.ToString(Formatting.None))
            )
            .Build();
        
        Guid id= await cmd.ExecuteScalarAsync<Guid>(cancellation).ConfigureAwait(false);
        return new StorageObject(obj.ContentType, id, 0, timeStamp, timeStamp, userName, userName, obj.Data);
    }

    // This is very back and forth, but maybe switching back to binary would be better for storage and retrieval speeds. In the end, maybe this should be a 
    // external choice.
    // https://learn.microsoft.com/da-dk/archive/blogs/sqlserverstorageengine/storing-json-in-sql-server#compressed-json-storage


    public Task<StorageObject> UpdateAsync(Guid id, JObject obj)
        => UpdateAsync(new UpdateStorageObject(string.Empty, id, obj), CancellationToken.None);

    public Task<StorageObject> UpdateAsync(Guid id, JObject obj, CancellationToken cancellation)
        => UpdateAsync(new UpdateStorageObject(string.Empty, id, obj), cancellation);

    public Task<StorageObject> UpdateAsync(UpdateStorageObject obj)
        => UpdateAsync(obj, CancellationToken.None);

    public async Task<StorageObject> UpdateAsync(UpdateStorageObject obj, CancellationToken cancellation)
    {
        await stateManager.Ensure();

        DateTime timeStamp = obj.Updated ?? DateTime.UtcNow;
        string userName = obj.UpdatedBy ?? context.UserInformation.UserName;

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
                ("user", userName),
                ("data", obj.Data.ToString(Formatting.None))
            )
            .Build();

        using ISqlServerDataReader<StorageObject> read = await cmd
            .ExecuteReaderAsync(
                new[] { "ContentType", "Version", "Created", "CreatedBy" },
                values => new StorageObject((string)values[0], obj.Id, (int)values[1], (DateTime)values[2], timeStamp, (string)values[3], userName, obj.Data),
                cancellation);

        return read.FirstOrDefault(); 
    }

    public Task<StorageObject?> DeleteAsync(Guid id)
        => DeleteAsync(id, CancellationToken.None);

    public Task<StorageObject?> DeleteAsync(Guid id, CancellationToken cancellation)
        => DeleteAsync(new DeleteStorageObject(string.Empty, id), cancellation);

    public  Task<StorageObject?> DeleteAsync(DeleteStorageObject obj)
        => DeleteAsync(obj, CancellationToken.None);

    public async Task<StorageObject?> DeleteAsync(DeleteStorageObject obj, CancellationToken cancellation)
    {
        await stateManager.Ensure();

        DateTime timeStamp = obj.Updated ?? DateTime.UtcNow;
        string userName = obj.UpdatedBy ?? context.UserInformation.UserName;

        using ISqlServerCommand cmd = context.CommandBuilder
            .From("DeleteDataTable")
            .Replace(
                ("schema", stateManager.Schema),
                ("data_table_name", $"{stateManager.AreaName}.data"),
                ("log_table_name", $"{stateManager.AreaName}.log")
            )
            .Parameters(
                ("id", obj.Id),
                ("timestamp", timeStamp),
                ("user", userName)
            )
            .Build();

        using ISqlServerDataReader<StorageObject> read = await cmd
            .ExecuteReaderAsync(
                new[] { "Id", "ContentType", "Version", "Created", "Updated", "CreatedBy", "UpdatedBy", "Data" },
                values => new StorageObject(
                    (string)values[1],
                    (Guid)values[0],
                    (int)values[2],
                    (DateTime)values[3],
                    (DateTime)values[4],
                    (string)values[5],
                    (string)values[6],
                    JObject.Parse((string)values[7])),
                CancellationToken.None);

        return read.FirstOrDefault();
    }


}