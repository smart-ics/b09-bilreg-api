using System.Text.Json;

namespace Bilreg.Infrastructure.Shared.Helpers;

public static class SystemTextJsonExtensions
{
    public static T DeserializeOrThrow<T>(this string json, string errorMessage, JsonSerializerOptions? options = null)
    {
        try
        {
            var result = JsonSerializer.Deserialize<T>(json, options);
            if (result == null)
            {
                throw new JsonException($"{errorMessage} - Deserialization resulted in null");
            }
            return result;
        }
        catch (JsonException ex)
        {
            throw new JsonException(errorMessage, ex);
        }
    }
    public static T DeserializeOrThrow<T>(this string json)
    {
        try
        {
            var result = JsonSerializer.Deserialize<T>(json);
            if (result == null)
            {
                throw new JsonException($"{json} - Deserialization resulted in null");
            }
            return result;
        }
        catch (JsonException ex)
        {
            throw new JsonException(ex.Message);
        }
    }
}
