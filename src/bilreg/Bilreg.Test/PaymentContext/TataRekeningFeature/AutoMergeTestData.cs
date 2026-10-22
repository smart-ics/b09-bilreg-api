using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

/// <summary>
/// Shared builders for the Inpatient Auto-Merge / Patient Identity Integrity tests (P2-S02).
/// </summary>
internal static class AutoMergeTestData
{
    public static RegModel RegWithNoRm(string regId, string noRm, JenisRegEnum jenisReg = JenisRegEnum.RegJalan) =>
        new(
            regId,
            new DateOnly(2026, 10, 12),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            jenisReg,
            new PasienReff(noRm, $"Pasien {noRm}", new DateOnly(1990, 1, 1), "L"),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);

    public static TataRekeningModel OpenedTrk(string regId) =>
        new(regId, TataRekeningStatusEnum.Opened, TataRekeningFinalizationType.Default, [], []);
}
