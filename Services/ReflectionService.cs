using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace blazor_wasm_app.Services;

public interface IReflectionService
{
    Dictionary<string, object?> GetEntityProperties<T>(T entity) where T : class;
    T CreateInstance<T>(Dictionary<string, object?> properties) where T : class, new();
    object? InvokeMethod<T>(T instance, string methodName, params object[] parameters) where T : class;
    IEnumerable<PropertyMetadata> GetPropertyMetadata<T>() where T : class;
    Func<T, TResult> CreatePropertyGetter<T, TResult>(string propertyName);
    Action<T, TValue> CreatePropertySetter<T, TValue>(string propertyName);
}

public class PropertyMetadata
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public Type Type { get; set; } = typeof(object);
    public bool IsNullable { get; set; }
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public List<string> Attributes { get; set; } = new();
}

public class ReflectionService : IReflectionService
{
    private readonly Dictionary<Type, PropertyInfo[]> _propertyCache = new();
    private readonly Dictionary<(Type, string), Delegate> _getterCache = new();
    private readonly Dictionary<(Type, string), Delegate> _setterCache = new();

    public Dictionary<string, object?> GetEntityProperties<T>(T entity) where T : class
    {
        var properties = GetCachedProperties(typeof(T));
        var result = new Dictionary<string, object?>();

        foreach (var prop in properties.Where(p => p.CanRead))
        {
            try
            {
                var value = prop.GetValue(entity);

                if (value != null && IsComplexType(prop.PropertyType))
                {
                    result[prop.Name] = JsonSerializer.Serialize(value);
                }
                else
                {
                    result[prop.Name] = value;
                }
            }
            catch
            {
                result[prop.Name] = null;
            }
        }

        return result;
    }

    public T CreateInstance<T>(Dictionary<string, object?> properties) where T : class, new()
    {
        var instance = new T();
        var props = GetCachedProperties(typeof(T));

        foreach (var kvp in properties)
        {
            var prop = props.FirstOrDefault(p => p.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
            if (prop?.CanWrite == true && kvp.Value != null)
            {
                try
                {
                    var convertedValue = ConvertValue(kvp.Value, prop.PropertyType);
                    prop.SetValue(instance, convertedValue);
                }
                catch
                {
                    // Skip properties that can't be set
                }
            }
        }

        return instance;
    }

    public object? InvokeMethod<T>(T instance, string methodName, params object[] parameters) where T : class
    {
        var method = typeof(T).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        if (method == null)
            throw new InvalidOperationException($"Method '{methodName}' not found on type '{typeof(T).Name}'");

        return method.Invoke(instance, parameters);
    }

    public IEnumerable<PropertyMetadata> GetPropertyMetadata<T>() where T : class
    {
        var properties = GetCachedProperties(typeof(T));

        return properties.Select(p => new PropertyMetadata
        {
            Name = p.Name,
            TypeName = GetFriendlyTypeName(p.PropertyType),
            Type = p.PropertyType,
            IsNullable = IsNullableType(p.PropertyType),
            CanRead = p.CanRead,
            CanWrite = p.CanWrite,
            Attributes = p.GetCustomAttributes()
                .Select(a => a.GetType().Name.Replace("Attribute", ""))
                .ToList()
        });
    }

    public Func<T, TResult> CreatePropertyGetter<T, TResult>(string propertyName)
    {
        var key = (typeof(T), propertyName);

        if (_getterCache.TryGetValue(key, out var cached))
            return (Func<T, TResult>)cached;

        var param = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(param, propertyName);
        var convert = Expression.Convert(property, typeof(TResult));
        var lambda = Expression.Lambda<Func<T, TResult>>(convert, param);
        var compiled = lambda.Compile();

        _getterCache[key] = compiled;
        return compiled;
    }

    public Action<T, TValue> CreatePropertySetter<T, TValue>(string propertyName)
    {
        var key = (typeof(T), propertyName);

        if (_setterCache.TryGetValue(key, out var cached))
            return (Action<T, TValue>)cached;

        var targetParam = Expression.Parameter(typeof(T), "target");
        var valueParam = Expression.Parameter(typeof(TValue), "value");
        var property = Expression.Property(targetParam, propertyName);
        var assign = Expression.Assign(property, Expression.Convert(valueParam, property.Type));
        var lambda = Expression.Lambda<Action<T, TValue>>(assign, targetParam, valueParam);
        var compiled = lambda.Compile();

        _setterCache[key] = compiled;
        return compiled;
    }

    private PropertyInfo[] GetCachedProperties(Type type)
    {
        if (!_propertyCache.TryGetValue(type, out var properties))
        {
            properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            _propertyCache[type] = properties;
        }
        return properties;
    }

    private static bool IsComplexType(Type type)
    {
        return !type.IsPrimitive
            && type != typeof(string)
            && type != typeof(decimal)
            && type != typeof(DateTime)
            && type != typeof(DateOnly)
            && type != typeof(Guid)
            && !type.IsEnum;
    }

    private static bool IsNullableType(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null
            || !type.IsValueType;
    }

    private static string GetFriendlyTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var genericName = type.Name.Split('`')[0];
            var typeArgs = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
            return $"{genericName}<{typeArgs}>";
        }

        return type.Name;
    }

    private static object? ConvertValue(object value, Type targetType)
    {
        if (value.GetType() == targetType)
            return value;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize(jsonElement.GetRawText(), targetType);
        }

        if (underlyingType.IsEnum && value is string strValue)
        {
            return Enum.Parse(underlyingType, strValue);
        }

        return Convert.ChangeType(value, underlyingType);
    }
}
