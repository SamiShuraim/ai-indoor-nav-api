using System.Text.Json;
using NetTopologySuite.Features;
using Newtonsoft.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace ai_indoor_nav_api;

using NetTopologySuite.Features;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class FeatureJsonConverter : JsonConverter<Feature>
{
    public override void WriteJson(JsonWriter writer, Feature? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("type");
        writer.WriteValue("Feature");

        // Write geometry
        writer.WritePropertyName("geometry");
        serializer.Serialize(writer, value?.Geometry);

        // Write properties
        writer.WritePropertyName("properties");
        writer.WriteStartObject();

        if (value?.Attributes != null)
        {
            foreach (var name in value.Attributes.GetNames())
            {
                writer.WritePropertyName(name);
                serializer.Serialize(writer, value.Attributes[name]);
            }
        }

        writer.WriteEndObject(); // properties
        writer.WriteEndObject(); // Feature
    }

    public override Feature? ReadJson(JsonReader reader, Type objectType, Feature? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException("Reading not implemented.");
    }

    public override bool CanRead => false;
}
