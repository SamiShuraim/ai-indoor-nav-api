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
                // Avoid including complex objects like navigation properties
                if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                    continue;

                attributes.Add(prop.Name.ToSnakeCase(), value); // optional: convert name to snake_case
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
                var coords = flattened.geometry["coordinates"] as JArray;
                if (coords != null && coords.Count == 2)
                {
                    var x = coords[0].ToObject<double>();
                    var y = coords[1].ToObject<double>();

                    var point = new NetTopologySuite.Geometries.Point(x, y) { SRID = 4326 };
                    if (geometryProp.PropertyType == typeof(NetTopologySuite.Geometries.Point))
                    {
                        geometryProp.SetValue(instance, point);
                    }
                }
            }

            foreach (var (key, value) in flattened.Props)
            {
                var prop = typeof(T).GetProperties()
                    .FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));

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
                        var converted = Convert.ChangeType(value, targetType);
                        prop.SetValue(instance, converted);
                    }
                }
                catch
                {
                    // Optional: log or skip conversion errors
                }
            }

            return instance;
        }
}