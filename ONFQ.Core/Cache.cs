using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ONFQ.ONFQ.Core;

/// <summary>
/// A concurrent cache to store objects during multi-threaded operations
/// </summary>
/// <typeparam name="TKey">The key type to store</typeparam>
/// <typeparam name="TValue">The value (usually float)</typeparam>
public class Cache<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> cache = [];
    private readonly Func<TKey, TValue> factory;

    public Cache(Func<TKey, TValue> factory)
    {
        this.factory = factory;
    }

    public TValue GetOrAdd(TKey key)
    {
        return cache.GetOrAdd(key, factory);
    }

    public bool TryGetValue(TKey key, out TValue? value)
    {
        return cache.TryGetValue(key, out value);
    }

    public void AddOrUpdate(TKey key, TValue value)
    {
        cache.AddOrUpdate(key, value, (_, _) => value);
    }

    public void Clear()
    {
        cache.Clear();
    }
}