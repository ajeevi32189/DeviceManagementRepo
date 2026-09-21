using System.Reflection;

namespace DeviceManagementOnly.Common.ImportExport
{
    public static class RowMapper
    {
        /// <summary>Reflection-sets every mapped header's value onto the target object's
        /// matching public settable property, converting text -> the property's type.</summary>
        public static void PopulateFromRow<T>(T target, IEnumerable<string> headers,
            Dictionary<string, string> row, Dictionary<string, string> headerToField)
        {
            var props = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var header in headers)
            {
                if (!headerToField.TryGetValue(header, out var fieldName)) continue;
                if (!props.TryGetValue(fieldName, out var prop)) continue;
                var text = row.TryGetValue(header, out var v) ? v : string.Empty;
                prop.SetValue(target, ConvertCell(prop.PropertyType, text));
            }
        }

        public static object? ConvertCell(Type targetType, string text)
        {
            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
            bool isNullable = Nullable.GetUnderlyingType(targetType) != null || !targetType.IsValueType;

            if (string.IsNullOrWhiteSpace(text))
                return isNullable ? null : Activator.CreateInstance(underlying);

            try
            {
                if (underlying == typeof(string)) return text;
                if (underlying == typeof(Guid)) return Guid.Parse(text);
                if (underlying == typeof(bool))
                {
                    var t = text.Trim().ToLowerInvariant();
                    return t is "1" or "yes" or "y" or "true";
                }
                if (underlying == typeof(DateTime)) return DateTime.Parse(text);
                if (underlying == typeof(decimal)) return decimal.Parse(text);
                if (underlying == typeof(double)) return double.Parse(text);
                if (underlying == typeof(float)) return float.Parse(text);
                if (underlying == typeof(int)) return int.Parse(text);
                if (underlying == typeof(long)) return long.Parse(text);
                if (underlying.IsEnum) return Enum.Parse(underlying, text, ignoreCase: true);

                return Convert.ChangeType(text, underlying);
            }
            catch
            {
                throw new InvalidOperationException($"Could not convert value '{text}' to {underlying.Name}.");
            }
        }
    }
}
