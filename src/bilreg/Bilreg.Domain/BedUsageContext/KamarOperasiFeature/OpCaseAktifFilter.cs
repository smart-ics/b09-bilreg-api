namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public record OpCaseAktifFilter(
    int? OpCaseState = null,
    DateTime? TglAwal = null,
    DateTime? TglAkhir = null,
    int? Limit = null);
