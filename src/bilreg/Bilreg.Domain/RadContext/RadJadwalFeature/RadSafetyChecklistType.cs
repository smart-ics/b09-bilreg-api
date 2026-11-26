namespace Bilreg.Domain.RadContext.RadJadwalFeature;

public record RadSafetyChecklistType(
    bool HasMetalImplant = false,
    bool HasContrastAllergy = false,
    bool IsPregnant = false,
    string Notes = "")
{
    public string Notes { get; init; } = Notes ?? string.Empty;

    public static RadSafetyChecklistType Default => new(
        HasMetalImplant: false,
        HasContrastAllergy: false,
        IsPregnant: false,
        Notes: string.Empty);
}
