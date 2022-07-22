namespace DotJEM.Json.Storage2.Cache;
public interface IAsyncCache<T> { }
public class AsyncCache<T>
{
    private readonly Dictionary<string, T> values = new();
    private readonly Dictionary<string, SemaphoreSlim> locks = new();

    public async Task<T> GetOrAdd(string key, Func<string, Task<T>> factory)
    {
        if (values.TryGetValue(key, out T value))
            return value;

        SemaphoreSlim? @lock;
        lock (locks)
        {
            if (!locks.TryGetValue(key, out @lock))
                locks.Add(key, @lock = new SemaphoreSlim(1, 1));
        }

        await @lock.WaitAsync().ConfigureAwait(false);
        if (values.TryGetValue(key, out value))
            return value;

        value = await factory(key);
        values.Add(key, value);

        lock(locks) locks.Remove(key);

        @lock.Release();
        return value;
    }

    public bool Release(string key)
    {
        return values.Remove(key);
    }
}