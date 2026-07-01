using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bilreg.Application.Shared.AuditLogFeature;

public static class AuditLogSnapshotJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize<T>(T entity)
        => JsonSerializer.Serialize(entity, Options);
}
