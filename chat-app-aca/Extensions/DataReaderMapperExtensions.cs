using System.Data.Common;
using System.Reflection;

namespace chat_app_aca.Extensions;

public static class DataReaderMapperExtensions
{
    public static T MapToEntity<T>(this DbDataReader reader) where T : new()
    {
        var entity = new T();

        var props = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        for (int i = 0; i < reader.FieldCount; i++)
        {
            var columnName = reader.GetName(i);
            var normalizedColumnName = Normalize(columnName);

            foreach (PropertyInfo propertyInfo in props)
            {
                if (Normalize(propertyInfo.Name) != normalizedColumnName)
                {
                    continue;
                }

                var value = reader.GetValue(i);
                if (value == DBNull.Value)
                {
                    break;
                }

                propertyInfo.SetValue(entity, ConvertValue(value, propertyInfo.PropertyType));
                break;
            }
        }

        return entity;
    }

    private static object? ConvertValue(object value, Type targetType)
    {
        var nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (nonNullableType == typeof(Guid))
        {
            return value is Guid guid ? guid : Guid.Parse(value.ToString());
        }

        if (nonNullableType.IsEnum)
        {
            return Enum.Parse(nonNullableType, value.ToString(), true);
        }

        if (nonNullableType == typeof(DateTime))
        {
            if (value is DateTime dt)
            {
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }

            if (value is DateTimeOffset dto)
            {
                return dto.UtcDateTime;
            }

            if (value is DateOnly dateOnly)
            {
                return dateOnly.ToDateTime(TimeOnly.MinValue);
            }
        }

        if (value is IConvertible)
        {
            return Convert.ChangeType(value, nonNullableType);
        }

        throw new InvalidCastException($"Cannot convert value of type {value.GetType()} to {nonNullableType}");
    }

    private static string Normalize(string name)
    {
        return name
            .Replace("_", "", StringComparison.OrdinalIgnoreCase)
            .ToLowerInvariant();
    }
}