using Bilreg.Domain.RadContext.RadOrderFeature;

namespace Bilreg.Application.RadContext.RadOrderFeature;

public record RadTarifModalityDto(
    string TarifId,
    string TarifName,
    string Modality,
    bool IsActive,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate)
{
    public RadTarifModalityDto() : this(
        string.Empty, string.Empty, string.Empty, true,
        string.Empty, new DateTime(3000, 1, 1), string.Empty, new DateTime(3000, 1, 1))
    {
    }

    public static RadTarifModalityDto FromModel(RadTarifModalityModel model, string user = "", DateTime? date = null)
    {
        var timestamp = date ?? new DateTime(3000, 1, 1);
        return new(
            model.TarifId,
            model.TarifName,
            model.Modality.ToString(),
            model.IsActive,
            user,
            timestamp,
            user,
            timestamp);
    }

    public RadTarifModalityModel ToModel()
    {
        var modality = Enum.TryParse<RadModalityEnum>(Modality, ignoreCase: true, out var m) ? m : RadModalityEnum.CR;
        return new RadTarifModalityModel(TarifId, TarifName, modality, IsActive);
    }
}
