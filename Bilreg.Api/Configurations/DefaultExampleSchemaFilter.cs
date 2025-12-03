using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Bilreg.Api.Configurations;

public class DefaultExampleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null)
            return;

        foreach (var prop in schema.Properties)
        {
            var propInfo = context.Type.GetProperty(prop.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propInfo == null)
                continue;

            schema.Properties[prop.Key].Example = GenerateExample(propInfo.PropertyType);
        }
    }

    private IOpenApiAny GenerateExample(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string))
            return new OpenApiString("");

        if (type == typeof(int) || type == typeof(long))
            return new OpenApiInteger(0);

        if (type == typeof(bool))
            return new OpenApiBoolean(false);

        if (type == typeof(DateTime))
            return new OpenApiString(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));

        if (type.Name == "TimeOnly")
            return new OpenApiString("00:00");

        if (type.Name == "DateOnly")
            return new OpenApiString(DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd"));

        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            return new OpenApiArray();

        return new OpenApiObject();
    }
}

