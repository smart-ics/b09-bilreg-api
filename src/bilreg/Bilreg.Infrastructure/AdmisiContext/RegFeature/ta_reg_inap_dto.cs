// ReSharper disable InconsistentNaming
using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public record ta_reg_inap_dto(
    string fs_kd_reg,
    string fs_kd_caramasuk_inap,
    string fs_kd_trs_booking_bed,
    string fs_kd_medis_sekunder)
{
    public const string LegacyBlank = " ";

    public static ta_reg_inap_dto FromModel(RegInapModel model)
        => new(
            model.RegId,
            model.ProsedurMasukInap.ProsedurMasukInapId,
            LegacyBlank,
            LegacyBlank);

    public static string NormalizeOptional(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed is "" or "-" ? string.Empty : trimmed;
    }

    public static string ToDbOptional(string? value)
    {
        var normalized = NormalizeOptional(value);
        return normalized.Length == 0 ? LegacyBlank : normalized;
    }
}
