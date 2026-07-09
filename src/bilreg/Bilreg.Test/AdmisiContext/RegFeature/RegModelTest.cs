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
        var listDokter = new List<RegDokterType>();
        if (dokter != null)
        {
            listDokter.Add(new RegDokterType(dokter, DateOnly.FromDateTime(DateTime.Now), true));
        }
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
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            [], listDokter.AsEnumerable());
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

    [Fact]
    public void Constructor_GivenRealDoctor_SeedsOneActiveDpjp()
    {
        var dokter = DokterRef("DR01", "Dr. Satu");
        var reg = CreateReg(dokter);

        reg.ListDokter.Should().HaveCount(1);
        var assignment = reg.ListDokter.Single();
        assignment.Dokter.Should().Be(dokter);
        assignment.IsPrimer.Should().BeTrue();
        assignment.IsActive.Should().BeTrue();
        //assignment.AssignDate.Should().Be(RegDate);
        assignment.ReleaseDate.Should().BeNull();
        reg.Dokter.Should().Be(dokter);
    }

    [Fact]
    public void Constructor_GivenDefaultDoctor_LeavesCollectionEmpty()
    {
        var reg = CreateReg();

        reg.ListDokter.Should().BeEmpty();
        reg.Dokter.PpaId.Should().Be("-");
    }

    [Fact]
    public void AssignDokter_GivenNewDoctor_CreatesInactiveAssignment()
    {
        var reg = CreateReg();
        var dokter = DokterRef("DR02", "Dr. Dua");

        reg.AssignDokter(dokter);

        reg.ListDokter.Should().HaveCount(1);
        var assignment = reg.ListDokter.Single();
        assignment.Dokter.Should().Be(dokter);
        assignment.IsPrimer.Should().BeFalse();
        assignment.IsActive.Should().BeTrue();
        assignment.AssignDate.Should().Be(Today);
        reg.Dokter.PpaId.Should().Be("-");
    }

    [Fact]
    public void AssignDokter_GivenActiveDoctor_Throws()
    {
        var dokter = DokterRef("DR01", "Dr. Satu");
        var reg = CreateReg(dokter);

        var act = () => reg.AssignDokter(dokter);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah memiliki penugasan aktif*");
    }

    [Fact]
    public void AssignDokter_AfterRelease_CreatesNewHistoricalRow()
    {
        var reg = CreateReg();
        var dokter = DokterRef("DR02", "Dr. Dua");
        reg.AssignDokter(dokter);
        reg.ReleaseDokter(dokter);

        reg.AssignDokter(dokter);

        reg.ListDokter.Should().HaveCount(2);
        reg.ListDokter.Count(x => x.IsActive).Should().Be(1);
        reg.ListDokter.Single(x => x.IsActive).AssignDate.Should().Be(Today);
    }

    [Fact]
    public void SetDpjp_GivenNoActiveAssignment_Throws()
    {
        var reg = CreateReg();
        var dokter = DokterRef("DR02", "Dr. Dua");

        var act = () => reg.SetDpjp(dokter);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*penugasan aktif*");
    }

    [Fact]
    public void SetDpjp_GivenActiveAssignments_EnsuresSingleActiveDpjp()
    {
        var reg = CreateReg();
        var dokterA = DokterRef("DR01", "Dr. Satu");
        var dokterB = DokterRef("DR02", "Dr. Dua");
        reg.AssignDokter(dokterA);
        reg.AssignDokter(dokterB);
        reg.SetDpjp(dokterA);

        reg.SetDpjp(dokterB);

        reg.Dokter.Should().Be(dokterB);
        reg.ListDokter.Count(x => x.IsActive && x.IsPrimer).Should().Be(1);
        reg.ListDokter.Single(x => x.Dokter.PpaId == "DR01").IsPrimer.Should().BeFalse();
        reg.ListDokter.Single(x => x.Dokter.PpaId == "DR02").IsPrimer.Should().BeTrue();
    }

    [Fact]
    public void ReleaseDokter_GivenActiveAssignment_SetsReleaseDateAndClearsPrimary()
    {
        var dokter = DokterRef("DR01", "Dr. Satu");
        var reg = CreateReg(dokter);

        reg.ReleaseDokter(dokter);

        var released = reg.ListDokter.Single();
        released.IsActive.Should().BeFalse();
        released.ReleaseDate.Should().Be(Today);
        released.IsPrimer.Should().BeFalse();
        reg.Dokter.PpaId.Should().Be("-");
    }

    [Fact]
    public void ReleaseDokter_GivenNoActiveAssignment_Throws()
    {
        var dokter = DokterRef("DR01", "Dr. Satu");
        var reg = CreateReg(dokter);
        reg.ReleaseDokter(dokter);

        var act = () => reg.ReleaseDokter(dokter);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tidak memiliki penugasan aktif*");
    }

    [Fact]
    public void AssignVisitTo_GivenDoctor_SetsActiveDpjp()
    {
        var reg = CreateReg();
        var dokter = TestDokter("DR03", "Dr. Tiga");
        var (layanan, karcis) = CreateRajalVisitData();

        reg.AssignVisitTo(dokter, layanan, karcis);

        reg.Dokter.PpaId.Should().Be("DR03");
        reg.ListDokter.Should().ContainSingle(x => x.IsActive && x.IsPrimer);
        reg.ListKomponen.Should().HaveCount(1);
    }

    [Fact]
    public void ChangeDataKunjungan_GivenNewDoctor_ReplacesActiveDpjp()
    {
        var existing = DokterRef("DR01", "Dr. Satu");
        var reg = CreateReg(existing);
        var newDokter = TestDokter("DR04", "Dr. Empat");
        var (layanan, karcis) = CreateRajalVisitData("LYN02");

        reg.ChangeDataKunjungan(layanan, karcis, newDokter);

        reg.Dokter.PpaId.Should().Be("DR04");
        reg.ListDokter.Should().HaveCount(2);
        reg.ListDokter.Single(x => x.Dokter.PpaId == "DR01").IsActive.Should().BeFalse();
        reg.ListDokter.Single(x => x.Dokter.PpaId == "DR04").IsPrimer.Should().BeTrue();
    }

    [Fact]
    public void ReleaseDokter_RetainsHistoricalAssignmentWithClearedPrimaryFlag()
    {
        var reg = CreateReg();
        var dokter = DokterRef("DR05", "Dr. Lima");
        reg.AssignDokter(dokter);
        reg.SetDpjp(dokter);

        reg.ReleaseDokter(dokter);

        var historical = reg.ListDokter.Single();
        historical.ReleaseDate.Should().Be(Today);
        historical.IsPrimer.Should().BeFalse();
    }
}
