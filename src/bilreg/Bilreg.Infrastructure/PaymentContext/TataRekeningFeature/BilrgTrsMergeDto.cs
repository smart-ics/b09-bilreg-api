namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

/// <summary>
/// Persistence DTO for the <c>ta_trs_merge</c> relation table.
/// <c>fs_kd_reg_merge</c> = inpatient main registration (RegRanap / InpatientRegId),
/// <c>fs_kd_reg_asal</c> = preceding registration (RegIGD / RegPoli / PrecedingRegId).
/// </summary>
public record BilrgTrsMergeDto(
    string MergeRegId,
    string AsalRegId,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate);
