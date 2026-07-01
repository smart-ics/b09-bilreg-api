using Bilreg.Domain.AdmisiContext.AntrianFeature;
using System.Text.Json;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public class AntrianPatternDto
{
    public string? Tipe { get; set; }
    public int Max { get; set; }
    public int Rsrvd { get; set; } = 0;
    public List<AntrianPatternItemDto>? Pttrn { get; set; }
}

public class AntrianPatternItemDto
{
    public string? Desc { get; set; }
    public int Qty { get; set; }
}

public static class AntrianPatternMapper
{
    public static AntrianPatternType ToDomain(this AntrianPatternDto dto)
        => new(
            dto.Tipe ?? string.Empty,
            dto.Max,
            dto.Rsrvd,
            dto.Pttrn?.Select(x => new AntrianPatternItemType(
                x.Desc ?? string.Empty,
                x.Qty
            )) ?? []
        );

    public static AntrianPatternDto ToDto(this AntrianPatternType model)
        => new()
        {
            Tipe = model.Tipe,
            Max = model.Max,
            Rsrvd = model.Rsrvd,
            Pttrn = model.Pttrn
                .Select(x => new AntrianPatternItemDto
                {
                    Desc = x.Desc,
                    Qty = x.Qty
                })
                .ToList()
        };
}


public static class AntrianPatternFactory
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    public static AntrianPatternType FromStringJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return AntrianPatternType.Default;

        return JsonSerializer.Deserialize<AntrianPatternDto>(json, _options)
            ?.ToDomain()
            ?? AntrianPatternType.Default;
    }

    public static string ToStringJson(AntrianPatternType model)
    {
        var dto = model.ToDto();
        return JsonSerializer.Serialize(dto);
    }
}