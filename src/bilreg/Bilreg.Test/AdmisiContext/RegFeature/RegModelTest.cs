using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegModelTest
{
    private static readonly DateOnly RegDate = new(2026, 6, 14);
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

    private static PpaReff DokterRef(string id, string name) => new(id, name);

    private static PpaType TestDokter(string id, string name)
    {
        var satTugas = new SatTugasType("ST1", "Dokter", ProfesiType.Dokter);
        return new PpaType(id, name, name, SmfType.Default, GroupSpesialisType.Default,
            listLayanan: [], listSatTugas: [new PpaSatTugasType(satTugas, true)], listContact: []);
    }

    private static RegModel CreateReg(PpaReff? dokter = null)
    {
        var result = new RegModel(
            "RG00000001",
            RegDate,
            new AuditInfoType("tester", new DateTime(2026, 6, 14, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
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
        return result;
    }

    private static (LayananType Layanan, KarcisType Karcis) CreateRajalVisitData(string layananId = "LYN01")
    {
        var layanan = LayananType.Default with
        {
            LayananId = layananId,
            LayananName = "Poli Umum",
            InstalasiDk = InstalasiDkType.RawatJalan
        };
        var karcis = new KarcisType(
            "KRC01", "Karcis Test", true,
            InstalasiDkType.RawatJalan,
            RekapCetakType.Default.ToReff(),
            TarifType.Default.ToReff(),
            [new KarcisKomponenType(KomponenType.Default.ToReff(), 10000m)],
            [layanan.ToReff()]);
        return (layanan, karcis);
    }
}
