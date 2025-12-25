using System.Collections.Concurrent;

namespace SharpAIKit.Common;

/// <summary>
/// Strongly-typed context object for type-safe data passing.
/// Replaces Dictionary<string, object?> with compile-time type checking.
/// </summary>
public class StrongContext
{
    private readonly ConcurrentDictionary<string, object?> _data = new();
    private readonly ConcurrentDictionary<Type, object> _typedData = new();

    /// <summary>
    /// Gets or sets a value by key (type-unsafe, for backward compatibility).
    /// Use Get&lt;T&gt; and Set&lt;T&gt; for type-safe access.
    /// </summary>
    public object? this[string key]
    {
        get => _data.TryGetValue(key, out var value) ? value : null;
        set
        {
            _data[key] = value;
            if (value != null)
            {
                _typedData[value.GetType()] = value;
            }
        }
    }

    /// <summary>
    /// Gets a typed value from the context.
    /// </summary>
    public T? Get<T>(string? key = null)
    {
        // Try typed storage first
        if (_typedData.TryGetValue(typeof(T), out var typedValue) && typedValue is T result)
        {
            return result;
        }

        // Try key-based lookup
        if (!string.IsNullOrEmpty(key) && _data.TryGetValue(key, out var value))
        {
            if (value is T typed)
            {
                return typed;
            }
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        // Try to find by type name
        var typeName = typeof(T).Name;
        if (_data.TryGetValue(typeName, out var namedValue) && namedValue is T namedTyped)
        {
            return namedTyped;
        }

        return default(T);
    }

    /// <summary>
    /// Sets a typed value in the context.
    /// </summary>
    public StrongContext Set<T>(T? value, string? key = null)
    {
        if (value == null) return this;

        _typedData[typeof(T)] = value;
        
        if (!string.IsNullOrEmpty(key))
        {
            _data[key] = value;
        }
        else
        {
            _data[typeof(T).Name] = value;
        }

        return this;
    }

    /// <summary>
    /// Sets a value with a key.
    /// </summary>
    public StrongContext Set(string key, object? value)
    {
        _data[key] = value;
        if (value != null)
        {
            _typedData[value.GetType()] = value;
        }
        return this;
    }

    /// <summary>
    /// Gets all keys in the context.
    /// </summary>
    public IEnumerable<string> Keys => _data.Keys;

    /// <summary>
    /// Checks if a key exists.
    /// </summary>
    public bool HasKey(string key) => _data.ContainsKey(key);

    /// <summary>
    /// Checks if a typed value exists.
    /// </summary>
    public bool Has<T>() => _typedData.ContainsKey(typeof(T));

    /// <summary>
    /// Removes a value by key.
    /// </summary>
    public bool Remove(string key)
    {
        if (_data.TryRemove(key, out var value) && value != null)
        {
            _typedData.TryRemove(value.GetType(), out _);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes a typed value.
    /// </summary>
    public bool Remove<T>()
    {
        var removed = _typedData.TryRemove(typeof(T), out _);
        var typeName = typeof(T).Name;
        _data.TryRemove(typeName, out _);
        return removed;
    }

    /// <summary>
    /// Clears all data.
    /// </summary>
    public void Clear()
    {
        _data.Clear();
        _typedData.Clear();
    }

    /// <summary>
    /// Creates a copy of this context.
    /// </summary>
    public StrongContext Clone()
    {
        var clone = new StrongContext();
        foreach (var kvp in _data)
        {
            clone._data[kvp.Key] = kvp.Value;
        }
        foreach (var kvp in _typedData)
        {
            clone._typedData[kvp.Key] = kvp.Value;
        }
        return clone;
    }

    /// <summary>
    /// Creates a new context from a dictionary.
    /// </summary>
    public static StrongContext FromDictionary(Dictionary<string, object?> data)
    {
        var context = new StrongContext();
        foreach (var kvp in data)
        {
            context.Set(kvp.Key, kvp.Value);
        }
        return context;
    }

    /// <summary>
    /// Serializes the context to JSON.
    /// </summary>
    public string ToJson()
    {
        var dict = _data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        return System.Text.Json.JsonSerializer.Serialize(dict);
    }

    /// <summary>
    /// Deserializes the context from JSON.
    /// </summary>
    public static StrongContext FromJson(string json)
    {
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
        return dict != null ? FromDictionary(dict) : new StrongContext();
    }
}

/// <summary>
/// Generic context with type-safe access to a specific data type.
/// </summary>
/// <typeparam name="T">The primary data type stored in this context.</typeparam>
public class StrongContext<T> : StrongContext
{
    /// <summary>
    /// Gets or sets the primary data.
    /// </summary>
    public T? Data
    {
        get => Get<T>();
        set => Set(value);
    }

    /// <summary>
    /// Creates a new typed context.
    /// </summary>
    public StrongContext(T? initialData = default)
    {
        if (initialData != null)
        {
            Data = initialData;
        }
    }
}

