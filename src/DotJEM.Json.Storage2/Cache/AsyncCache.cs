using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DotJEM.Json.Storage2.Cache;
/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IAsyncCache<T> { }

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class AsyncCache<T>
{
    private readonly Dictionary<string, T> values = new();
    private readonly Dictionary<string, SemaphoreSlim> locks = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="factory"></param>
    /// <returns></returns>
    public async Task<T> GetOrAdd(string key, Func<string, Task<T>> factory)
    {
        if (values.TryGetValue(key, out T value))
            return value;

        SemaphoreSlim? @lock;
        lock (locks)
        {
            if (!locks.TryGetValue(key, out @lock))
                locks.Add(key, @lock = new (1, 1));
        }

        Debug.Assert(@lock != null);
        await @lock.WaitAsync().ConfigureAwait(false);
        if (values.TryGetValue(key, out value))
            return value;

        value = await factory(key);
        values.Add(key, value);

        lock(locks) locks.Remove(key);

        @lock.Release();
        return value;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool Release(string key)
    {
        return values.Remove(key);
    }
}