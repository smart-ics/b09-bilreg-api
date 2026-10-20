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

    [Fact]
    public void GivenActiveRegistration_WhenBatalBerobat_ThenUsesSuppliedTimestampAndBecomesInactive()
    {
        var registration = CreateReg();
        var timestamp = new DateTime(2026, 7, 12, 10, 30, 0);

        registration.BatalBerobat("void-user", timestamp);

        registration.RegVoidAudit.Should().Be(new AuditInfoType("void-user", timestamp));
        registration.RegKeluarAudit.Should().Be(AuditInfoType.Default);
        registration.RegCancelOutAudit.Should().Be(AuditInfoType.Default);
        registration.IsAktif.Should().BeFalse();
    }

    [Fact]
    public void GivenVoidedRegistration_WhenBatalBerobatAgain_ThenThrows()
    {
        var registration = CreateReg();
        registration.BatalBerobat("void-user", new DateTime(2026, 7, 12));

        var act = () => registration.BatalBerobat("void-user", new DateTime(2026, 7, 13));

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah dibatalkan*");
    }

    [Fact]
    public void GivenDischargedRegistration_WhenBatalBerobat_ThenThrows()
    {
        var registration = new RegModel(
            "RG00000002", RegDate,
            new AuditInfoType("tester", new DateTime(2026, 6, 14, 8, 0, 0)),
            new AuditInfoType("discharge-user", new DateTime(2026, 6, 15, 8, 0, 0)),
            AuditInfoType.Default, AuditInfoType.Default,
            JenisRegEnum.RegJalan, PasienModel.Default.ToReff(), TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(), KelasType.Default.ToReff(), CaraMasukDkType.Default,
            RujukanType.Default.ToReff(), PpaType.Default.ToReff(), LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(), RegEligibilityType.Default, []);

        var act = () => registration.BatalBerobat("void-user", new DateTime(2026, 7, 12));

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah keluar*");
    }

    [Fact]
    public void GivenActiveRegistration_WhenSetKeluar_ThenUpdatesRegKeluarAuditAndBecomesInactive()
    {
        var registration = CreateReg();
        var timestamp = new DateTime(2026, 7, 12, 14, 0, 0);

        registration.SetKeluar("discharge-user", timestamp);

        registration.RegKeluarAudit.Should().Be(new AuditInfoType("discharge-user", timestamp));
        registration.RegCancelOutAudit.Should().Be(AuditInfoType.Default);
        registration.RegVoidAudit.Should().Be(AuditInfoType.Default);
        registration.IsAktif.Should().BeFalse();
    }

    [Fact]
    public void GivenDischargedRegistration_WhenSetKeluarAgain_ThenThrows()
    {
        var registration = CreateReg();
        registration.SetKeluar("user-1", new DateTime(2026, 7, 12, 14, 0, 0));

        var act = () => registration.SetKeluar("user-2", new DateTime(2026, 7, 12, 15, 0, 0));

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah tercatat keluar*");
    }

    [Fact]
    public void GivenVoidedRegistration_WhenSetKeluar_ThenThrows()
    {
        var registration = CreateReg();
        registration.BatalBerobat("void-user", new DateTime(2026, 7, 12, 10, 0, 0));

        var act = () => registration.SetKeluar("discharge-user", new DateTime(2026, 7, 12, 14, 0, 0));

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah dibatalkan*");
    }

    [Fact]
    public void GivenDischargedRegistration_WhenCancelKeluar_ThenUpdatesRegCancelOutAuditAndBecomesActive()
    {
        var registration = CreateReg();
        var dischargeTime = new DateTime(2026, 7, 12, 14, 0, 0);
        var cancelTime = new DateTime(2026, 7, 12, 15, 30, 0);

        registration.SetKeluar("discharge-user", dischargeTime);
        registration.IsAktif.Should().BeFalse();

        registration.CancelKeluar("cancel-user", cancelTime);

        registration.RegKeluarAudit.Should().Be(AuditInfoType.Default);
        registration.RegCancelOutAudit.Should().Be(new AuditInfoType("cancel-user", cancelTime));
        registration.RegVoidAudit.Should().Be(AuditInfoType.Default);
        registration.IsAktif.Should().BeTrue();
    }

    [Fact]
    public void GivenActiveRegistration_WhenCancelKeluar_ThenThrows()
    {
        var registration = CreateReg();

        var act = () => registration.CancelKeluar("cancel-user", new DateTime(2026, 7, 12, 15, 30, 0));

        act.Should().Throw<InvalidOperationException>().WithMessage("*belum berstatus keluar*");
    }

    [Fact]
    public void GivenVoidedRegistration_WhenCancelKeluar_ThenThrows()
    {
        var registration = CreateReg();
        registration.BatalBerobat("void-user", new DateTime(2026, 7, 12, 10, 0, 0));

        var act = () => registration.CancelKeluar("cancel-user", new DateTime(2026, 7, 12, 15, 30, 0));

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah dibatalkan*");
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
