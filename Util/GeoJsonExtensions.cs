using System.Collections;
using NetTopologySuite.Geometries;
using System.Reflection;
using System.Text.Json;
using NetTopologySuite.Features;
using Newtonsoft.Json.Linq;

public static class GeoJsonExtensions
{
    public static Feature ToGeoJsonFeature<T>(this T obj) where T : class
    {
        if (obj == null) return null;

        var type = obj.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Geometry geometry = null;
        var attributes = new AttributesTable();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj);

            // If it's the geometry, store it for later
            if (value is Geometry geom)
            {
                geometry = geom;
            }
            else
            {
                // Avoid including complex objects like navigation properties, but allow collections of simple types
                if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                {
                    // Allow arrays and IEnumerable<T> where T is a simple type
                    var elementType = GetEnumerableElementType(prop.PropertyType);
                    var isEnumerableOfSimple = elementType != null && IsSimpleType(elementType);
                    var isArrayOfSimple = prop.PropertyType.IsArray && IsSimpleType(prop.PropertyType.GetElementType());

                    if (!(isEnumerableOfSimple || isArrayOfSimple))
                    {
                        continue;
                    }
                }

                attributes.Add(prop.Name.ToSnakeCase(), value);
            }
        }

        if (geometry == null)
            return null;

        return new Feature(geometry, attributes);
    }
    
    public static FeatureCollection ToGeoJsonFeatureCollection<T>(this IEnumerable<T> items) where T : class
    {
        var featureCollection = new FeatureCollection();

        foreach (var item in items)
        {
            var feature = item.ToGeoJsonFeature();
            if (feature != null)
            {
                featureCollection.Add(feature);
            }
        }

        return featureCollection;
    }

    private static bool IsSimpleType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = Nullable.GetUnderlyingType(type);
        }

        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(Guid)
            || type == typeof(double)
            || type == typeof(float);
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        var enumerableInterface = type
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments().FirstOrDefault();
    }

    public static void PopulateFromJson(this object target, JsonElement json)
    {
        var type = target.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in json.EnumerateObject())
        {
            var property = properties.FirstOrDefault(p =>
                string.Equals(p.Name, prop.Name, StringComparison.OrdinalIgnoreCase) &&
                p.CanWrite);

            if (property == null) continue;

            try
            {
                object? value = null;

                if (prop.Value.ValueKind == JsonValueKind.Null)
                {
                    if (IsNullable(property.PropertyType))
                        property.SetValue(target, null);
                    continue;
                }

                var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                value = prop.Value.ValueKind switch
                {
                    JsonValueKind.String when targetType == typeof(Guid) => Guid.Parse(prop.Value.GetString()!),
                    JsonValueKind.String when targetType == typeof(DateTime) => DateTime.Parse(prop.Value.GetString()!),
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number when targetType == typeof(int) => prop.Value.GetInt32(),
                    JsonValueKind.Number when targetType == typeof(double) => prop.Value.GetDouble(),
                    JsonValueKind.Number when targetType == typeof(decimal) => prop.Value.GetDecimal(),
                    JsonValueKind.Number when targetType == typeof(long) => prop.Value.GetInt64(),
                    JsonValueKind.True or JsonValueKind.False when targetType == typeof(bool) => prop.Value.GetBoolean(),
                    _ => JsonSerializer.Deserialize(prop.Value.GetRawText(), targetType)
                };

                if (value != null)
                    property.SetValue(target, value);
            }
            catch
            {
                // Optional: log or silently skip mismatches
            }
        }
    }
    
    public static (JObject? geometry, Dictionary<string, object?> properties) FlattenGeoJson(this JObject json)
    {
        if (json == null)
            throw new ArgumentNullException(nameof(json));

        if (!json.TryGetValue("geometry", out JToken? geometry))
            throw new InvalidOperationException("Expected GeoJSON object at root.");

        if (!json.TryGetValue("properties", out JToken? properties))
            throw new InvalidOperationException("GeoJSON properties missing.");

        var propsDict = new Dictionary<string, object?>();

        foreach (var prop in properties.Children<JProperty>())
        {
            propsDict[prop.Name] = prop.Value.Type == JTokenType.Null ? null : prop.Value.ToObject<object>();
        }

        return ((JObject)geometry, propsDict);
    }
    
    private static bool IsNullable(Type type) =>
        !type.IsValueType || (Nullable.GetUnderlyingType(type) != null);

    // Optional: Convert PascalCase to snake_case
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return System.Text.RegularExpressions.Regex.Replace(
            input,
            "(?<!^)([A-Z])",
            "_$1",
            System.Text.RegularExpressions.RegexOptions.Compiled
        ).ToLower();
    }
    public static T FromFlattened<T>(this (JObject? geometry, Dictionary<string, object?> Props) flattened) where T : new()
    {
        var instance = new T();

        // Handle geometry if T has a 'Geometry' property
        var geometryProp = typeof(T).GetProperty("Geometry");
        if (flattened.geometry != null && geometryProp != null && geometryProp.CanWrite)
        {
            var geomType = flattened.geometry["type"]?.ToString();
            var coordsToken = flattened.geometry["coordinates"];

            if (geomType == "Point" && coordsToken is JArray pointCoords && pointCoords.Count == 2)
            {
                var x = pointCoords[0].ToObject<double>();
                var y = pointCoords[1].ToObject<double>();

                var point = new Point(x, y) { SRID = 4326 };
                if (geometryProp.PropertyType == typeof(Point))
                {
                    geometryProp.SetValue(instance, point);
                }
            }
            else if (geomType == "Polygon" && coordsToken is JArray polygonCoords)
            {
                // polygonCoords: [[[x1, y1], [x2, y2], ..., [x1, y1]]]
                var shell = polygonCoords.First as JArray;
                if (shell != null)
                {
                    var coordinates = shell
                        .Select(c =>
                        {
                            var arr = c as JArray;
                            return new Coordinate(arr[0].ToObject<double>(), arr[1].ToObject<double>());
                        })
                        .ToArray();

                    var linearRing = new LinearRing(coordinates);
                    var polygon = new Polygon(linearRing) { SRID = 4326 };

                    if (geometryProp.PropertyType == typeof(Polygon))
                    {
                        geometryProp.SetValue(instance, polygon);
                    }
                }
            }
        }

        foreach (var (key, value) in flattened.Props)
        {
            var prop = typeof(T).GetProperties()
                .FirstOrDefault(p =>
                    string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(NormalizeName(p.Name), NormalizeName(key), StringComparison.OrdinalIgnoreCase));

            if (prop == null || !prop.CanWrite) continue;

            try
            {
                if (value == null)
                {
                    prop.SetValue(instance, null);
                }
                else
                {
                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    var converted = ConvertToType(value, targetType);
                    if (converted != null || IsNullable(targetType))
                    {
                        prop.SetValue(instance, converted);
                    }
                }
            }
            catch
            {
                // Optional: log or skip conversion errors
            }
        }

        return instance;
    }

    private static object? ConvertToType(object value, Type targetType)
    {
        // Handle lists and arrays of simple types
        var elementType = GetEnumerableElementType(targetType);
        var isEnumerableTarget = elementType != null && targetType != typeof(string);

        if (isEnumerableTarget)
        {
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;

            if (value is JArray jArray)
            {
                foreach (var token in jArray)
                {
                    var elemObj = token?.ToObject<object?>();
                    var convertedElem = elemObj == null ? null : ConvertToSimple(elemObj, elementType);
                    list.Add(convertedElem);
                }
            }
            else if (value is IEnumerable enumerable && value is not string)
            {
                foreach (var item in enumerable)
                {
                    var convertedElem = item == null ? null : ConvertToSimple(item, elementType);
                    list.Add(convertedElem);
                }
            }
            else
            {
                // Single value provided for a collection property
                var convertedElem = ConvertToSimple(value, elementType);
                list.Add(convertedElem);
            }

            // If the target is an array, convert list to array
            if (targetType.IsArray)
            {
                var array = Array.CreateInstance(elementType, list.Count);
                list.CopyTo(array, 0);
                return array;
            }

            return list;
        }

        // Handle simple scalars
        return ConvertToSimple(value, targetType);
    }

    private static object? ConvertToSimple(object value, Type targetType)
    {
        try
        {
            if (value is JValue jv)
            {
                value = jv.Value;
            }

            if (targetType == typeof(Guid)) return Guid.Parse(value.ToString()!);
            if (targetType == typeof(DateTime)) return DateTime.Parse(value.ToString()!);
            if (targetType.IsEnum) return Enum.Parse(targetType, value.ToString()!, true);

            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeName(string name) => name.Replace("_", "");
}