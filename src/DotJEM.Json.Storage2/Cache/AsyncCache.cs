namespace DotJEM.Json.Storage2.Cache;
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