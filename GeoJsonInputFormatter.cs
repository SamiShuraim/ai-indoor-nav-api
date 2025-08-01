namespace ai_indoor_nav_api;

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

public class GeoJsonInputFormatter : TextInputFormatter
{
    private readonly GeoJsonReader _geoJsonReader;

    public GeoJsonInputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
        SupportedEncodings.Add(System.Text.Encoding.UTF8);
        _geoJsonReader = new GeoJsonReader();
    }

    protected override bool CanReadType(Type type)
    {
        return typeof(Geometry).IsAssignableFrom(type);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, System.Text.Encoding encoding)
    {
        using var reader = new StreamReader(context.HttpContext.Request.Body, encoding);
        var body = await reader.ReadToEndAsync();

        try
        {
            var geometry = _geoJsonReader.Read<Geometry>(body);
            return await InputFormatterResult.SuccessAsync(geometry);
        }
        catch (Exception ex)
        {
            context.ModelState.AddModelError(context.ModelName, "Invalid GeoJSON: " + ex.Message);
            return await InputFormatterResult.FailureAsync();
        }
    }
}
