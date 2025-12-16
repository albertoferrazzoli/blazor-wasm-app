using System.Linq.Expressions;
using System.Reflection;

namespace blazor_wasm_app.Services;

public interface IDynamicQueryService
{
    IEnumerable<T> ApplyFilter<T>(IEnumerable<T> source, string propertyName, string operation, object? value);
    IEnumerable<T> ApplySort<T>(IEnumerable<T> source, string propertyName, bool descending = false);
    IEnumerable<T> ApplyPaging<T>(IEnumerable<T> source, int page, int pageSize);
    Expression<Func<T, bool>> BuildPredicate<T>(IEnumerable<FilterCriteria> filters);
}

public class FilterCriteria
{
    public string PropertyName { get; set; } = string.Empty;
    public string Operation { get; set; } = "equals";
    public object? Value { get; set; }
    public string? LogicalOperator { get; set; } = "and";
}

public class DynamicQueryService : IDynamicQueryService
{
    public IEnumerable<T> ApplyFilter<T>(IEnumerable<T> source, string propertyName, string operation, object? value)
    {
        var predicate = BuildSinglePredicate<T>(propertyName, operation, value);
        return source.Where(predicate.Compile());
    }

    public IEnumerable<T> ApplySort<T>(IEnumerable<T> source, string propertyName, bool descending = false)
    {
        var param = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(param, propertyName);
        var convert = Expression.Convert(property, typeof(object));
        var lambda = Expression.Lambda<Func<T, object>>(convert, param);
        var compiled = lambda.Compile();

        return descending
            ? source.OrderByDescending(compiled)
            : source.OrderBy(compiled);
    }

    public IEnumerable<T> ApplyPaging<T>(IEnumerable<T> source, int page, int pageSize)
    {
        return source.Skip((page - 1) * pageSize).Take(pageSize);
    }

    public Expression<Func<T, bool>> BuildPredicate<T>(IEnumerable<FilterCriteria> filters)
    {
        var filterList = filters.ToList();
        if (!filterList.Any())
        {
            return x => true;
        }

        Expression<Func<T, bool>>? combined = null;

        foreach (var filter in filterList)
        {
            var predicate = BuildSinglePredicate<T>(filter.PropertyName, filter.Operation, filter.Value);

            if (combined == null)
            {
                combined = predicate;
            }
            else
            {
                combined = filter.LogicalOperator?.ToLowerInvariant() == "or"
                    ? CombineOr(combined, predicate)
                    : CombineAnd(combined, predicate);
            }
        }

        return combined ?? (x => true);
    }

    private Expression<Func<T, bool>> BuildSinglePredicate<T>(string propertyName, string operation, object? value)
    {
        var param = Expression.Parameter(typeof(T), "x");
        var property = GetNestedPropertyExpression(param, propertyName);

        Expression body = operation.ToLowerInvariant() switch
        {
            "equals" or "eq" => Expression.Equal(property, Expression.Constant(value, property.Type)),
            "notequals" or "ne" => Expression.NotEqual(property, Expression.Constant(value, property.Type)),
            "contains" => BuildContainsExpression(property, value?.ToString() ?? ""),
            "startswith" => BuildStartsWithExpression(property, value?.ToString() ?? ""),
            "endswith" => BuildEndsWithExpression(property, value?.ToString() ?? ""),
            "gt" or "greaterthan" => Expression.GreaterThan(property, Expression.Constant(value, property.Type)),
            "gte" or "greaterthanorequal" => Expression.GreaterThanOrEqual(property, Expression.Constant(value, property.Type)),
            "lt" or "lessthan" => Expression.LessThan(property, Expression.Constant(value, property.Type)),
            "lte" or "lessthanorequal" => Expression.LessThanOrEqual(property, Expression.Constant(value, property.Type)),
            "isnull" => Expression.Equal(property, Expression.Constant(null)),
            "isnotnull" => Expression.NotEqual(property, Expression.Constant(null)),
            _ => throw new NotSupportedException($"Operation '{operation}' is not supported")
        };

        return Expression.Lambda<Func<T, bool>>(body, param);
    }

    private static Expression GetNestedPropertyExpression(Expression param, string propertyPath)
    {
        var properties = propertyPath.Split('.');
        Expression current = param;

        foreach (var prop in properties)
        {
            current = Expression.Property(current, prop);
        }

        return current;
    }

    private static Expression BuildContainsExpression(Expression property, string value)
    {
        var method = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
        var constant = Expression.Constant(value);
        return Expression.Call(property, method, constant);
    }

    private static Expression BuildStartsWithExpression(Expression property, string value)
    {
        var method = typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!;
        var constant = Expression.Constant(value);
        return Expression.Call(property, method, constant);
    }

    private static Expression BuildEndsWithExpression(Expression property, string value)
    {
        var method = typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!;
        var constant = Expression.Constant(value);
        return Expression.Call(property, method, constant);
    }

    private static Expression<Func<T, bool>> CombineAnd<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var param = Expression.Parameter(typeof(T), "x");
        var body = Expression.AndAlso(
            Expression.Invoke(left, param),
            Expression.Invoke(right, param));
        return Expression.Lambda<Func<T, bool>>(body, param);
    }

    private static Expression<Func<T, bool>> CombineOr<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var param = Expression.Parameter(typeof(T), "x");
        var body = Expression.OrElse(
            Expression.Invoke(left, param),
            Expression.Invoke(right, param));
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}
