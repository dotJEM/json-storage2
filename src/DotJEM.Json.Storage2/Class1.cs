using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Storage2;

public static class SqlServerStatements
{
    public static string Load(string input, params (string key, string value)[] values)
        => Load(input, values.ToDictionary(kv => kv.key, kv => kv.value));
    public static string Load(string resource, IDictionary<string, string> values)
    {
        return Replacer.Replace(Resources.Load($"DotJEM.Json.Storage2.SqlServer.Statements.{resource}.sql"), values);
    }
}

public class Replacer
{
    private static readonly Regex pattern = new("@\\{(.+?)}", RegexOptions.Compiled);

    public static string Replace(string input, params (string key, string value)[] values)
        => Replace(input, values.ToDictionary(kv => kv.key, kv => kv.value));

    public static string Replace(string input, IDictionary<string, string> values)
    {
        return pattern.Replace(input, match =>
        {
            string key = match.Groups[1].ToString();
            return values[key];
        });
    }
}

public class Resources
{
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();

    public static string Load(string resource)
    {
        // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
        string[] names = assembly.GetManifestResourceNames();

        using Stream? resourceStream = assembly.GetManifestResourceStream(resource);
        if (resourceStream == null)
            throw new FileNotFoundException();

        using StreamReader reader = new(resourceStream);
        return reader.ReadToEnd();
    }
}



public interface IAsyncCache<T> { }
public class AsyncCache<T>
{
    private readonly Dictionary<string, T> values = new();
    private readonly Dictionary<string, Mutex> locks = new();

    public async Task<T> GetOrAdd(string key, Func<string, Task<T>> factory)
    {
        T value;
        if (values.TryGetValue(key, out value))
            return value;

        Mutex @lock;
        lock (locks)
        {
            if (!locks.TryGetValue(key, out @lock))
                locks.Add(key, @lock = new Mutex());
        }

        @lock.WaitOne();
        if (values.TryGetValue(key, out value))
            return value;

        value = await factory(key);
        values.Add(key, value);

        locks.Remove(key);
        @lock.ReleaseMutex();
        return value;
    }

    public bool Release(string key)
    {
        lock (values)
        {
            return values.Remove(key);
        }
    }
}
public interface IStorageContext { }

public class SqlServerStorageContext : IStorageContext
{
    private readonly string connectionString;
    private readonly AsyncCache<SqlServerStorageArea> areas = new();
    private readonly SqlServerStorageAreaFactory factory;

    public SqlServerStorageContext(string connectionString)
    {
        this.connectionString = connectionString;
        factory = new SqlServerStorageAreaFactory(this);
    }

    public async Task<IStorageArea> AreaAsync(string name)
    {
        return await areas.GetOrAdd(name, key => factory.Create(key));
    }

    public bool Release(string name)
    {
        return areas.Release(name);
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(connectionString);
    }
}

public interface IStorageArea
{
    string Name { get; }

    IAsyncEnumerable<StorageObject> GetAsync();
    IAsyncEnumerable<StorageObject> GetAsync(long skip, int take = 100);

    Task<StorageObject> GetAsync(Guid id);
    Task<StorageObject> InsertAsync(StorageObject obj);
    Task<StorageObject> UpdateAsync(Guid id, StorageObject obj);
    Task<StorageObject> DeleteAsync(Guid id);
}

public class SqlServerStorageArea : IStorageArea
{
    public string Name { get; }

    public SqlServerStorageArea(SqlServerStorageContext sqlServerStorageContext, string name)
    {
        this.Name = name;
    }

    public IAsyncEnumerable<StorageObject> GetAsync()
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<StorageObject> GetAsync(long skip, int take = 100)
    {
        throw new NotImplementedException();
    }

    public Task<StorageObject> GetAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<StorageObject> InsertAsync(StorageObject obj)
    {
        throw new NotImplementedException();
    }

    public Task<StorageObject> UpdateAsync(Guid id, StorageObject obj)
    {
        throw new NotImplementedException();
    }

    public Task<StorageObject> DeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }


}

public readonly record struct StorageObject(Guid Id, int Version, DateTime Created, DateTime Updated, JObject Data);