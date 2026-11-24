namespace Bilreg.Domain.RadContext.RadOrderFeature;

public record RadTarifModalityModel(
    string TarifId,
    string TarifName,
    RadModalityEnum Modality,
    bool IsActive = true)
{
    public static RadTarifModalityModel Create(
        string tarifId,
        string tarifName,
        RadModalityEnum modality,
        bool isActive = true)
        => new(
            tarifId ?? string.Empty,
            tarifName ?? string.Empty,
            modality,
            isActive);
}
