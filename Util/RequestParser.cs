using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ai_indoor_nav_api;

public static class RequestParser
{
    public static async Task<(bool Success, string? ErrorMessage, TEntity? Entity)> TryParseFlattenedEntity<TEntity>(
        HttpRequest request
    ) where TEntity : new()
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(body))
            return (false, "Request body is empty", default(TEntity));

        JObject jsonObject;
        try
        {
            jsonObject = JObject.Parse(body);
        }
        catch (JsonReaderException ex)
        {
            return (false, "Invalid JSON format: " + ex.Message, default(TEntity));
        }

        try
        {
            var flattened = jsonObject.FlattenGeoJson();
            var entity = flattened.FromFlattened<TEntity>();
            return (true, null, entity);
        }
        catch (Exception ex)
        {
            return (false, "Failed to map entity: " + ex.Message, default(TEntity));
        }
    }
}