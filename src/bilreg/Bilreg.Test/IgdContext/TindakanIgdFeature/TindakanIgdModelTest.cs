using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.IgdContext.TindakanIgdFeature;

public class TindakanIgdModelTest
{
    private static AuditInfoType TestAudit() => new("U1", new DateTime(2026, 1, 1, 8, 0, 0));

    private static VisitorType TestVisitor() => new(
        VisitorName: "Pasien Tes",
        Gender: "L",
        TglLahir: new DateOnly(1990, 1, 1),
        Kontak: "0812000");

    private static PpaType TestDokter()
    {
        var smf = SmfType.Default;
        var groupSpesialis = GroupSpesialisType.Default;
        var satTugas = new SatTugasType("ST1", "Dokter", ProfesiType.Dokter);
        var listSatTugas = new[] { new PpaSatTugasType(satTugas, true) };
        return new PpaType("DK1", "Dr. Tes", "Tes", smf, groupSpesialis,
            listLayanan: [], listSatTugas: listSatTugas, listContact: []);
    }

    [Fact]
    public void Create_BeforeRegistration_ProducesModelWithUnregisteredRegIdAndUnpostedStatus()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        var tdk = TindakanIgdModel.Create(
            visit,
            reffId: "TDK001",
            desciption: "Injeksi Obat",
            qty: 1,
            aktifitas: ActivityTindakanIgd.Tindakan,
            ppa: TestDokter(),
            userId: "U1",
            occurredAt: new DateTime(2026, 1, 1, 8, 30, 0));

        tdk.TindakanIgdId.Should().StartWith("TKI");
        tdk.IgdVisitId.Should().Be(visit.IgdVisitId);
        tdk.RegId.Should().Be("-");
        tdk.IsPosted.Should().BeFalse();
        tdk.TrsTdkId.Should().BeEmpty();
        tdk.TrsBillId.Should().BeEmpty();
        tdk.PostedDate.Should().BeNull();
    }

    [Fact]
    public void AssignRegister_GivenValidRegId_UpdatesRegId()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        var tdk = TindakanIgdModel.Create(
            visit,
            reffId: "TDK001",
            desciption: "Injeksi Obat",
            qty: 1,
            aktifitas: ActivityTindakanIgd.Tindakan,
            ppa: TestDokter(),
            userId: "U1");

        tdk.AssignRegister("REG12345");
        tdk.RegId.Should().Be("REG12345");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AssignRegister_GivenEmptyOrNull_ThrowsArgumentException(string? invalidRegId)
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        var tdk = TindakanIgdModel.Create(
            visit,
            reffId: "TDK001",
            desciption: "Injeksi Obat",
            qty: 1,
            aktifitas: ActivityTindakanIgd.Tindakan,
            ppa: TestDokter(),
            userId: "U1");

        var act = () => tdk.AssignRegister(invalidRegId!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkAsPosted_GivenTrsIdsAndDate_UpdatesPostingStatus()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        var tdk = TindakanIgdModel.Create(
            visit,
            reffId: "TDK001",
            desciption: "Injeksi Obat",
            qty: 1,
            aktifitas: ActivityTindakanIgd.Tindakan,
            ppa: TestDokter(),
            userId: "U1");

        var postedAt = new DateTime(2026, 1, 1, 9, 0, 0);
        tdk.MarkAsPosted("TTDK001", "TBILL001", postedAt);

        tdk.IsPosted.Should().BeTrue();
        tdk.TrsTdkId.Should().Be("TTDK001");
        tdk.TrsBillId.Should().Be("TBILL001");
        tdk.PostedDate.Should().Be(postedAt);
    }

    [Theory]
    [InlineData("", "TBILL001")]
    [InlineData("TTDK001", "")]
    [InlineData(null, "TBILL001")]
    [InlineData("TTDK001", null)]
    public void MarkAsPosted_GivenInvalidIds_ThrowsArgumentException(string? trsTdkId, string? trsBillId)
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        var tdk = TindakanIgdModel.Create(
            visit,
            reffId: "TDK001",
            desciption: "Injeksi Obat",
            qty: 1,
            aktifitas: ActivityTindakanIgd.Tindakan,
            ppa: TestDokter(),
            userId: "U1");

        var act = () => tdk.MarkAsPosted(trsTdkId!, trsBillId!, DateTime.Now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkAsVoided_GivenValidAuditAndReason_Succeeds()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        var tdk = TindakanIgdModel.Create(
            visit,
            reffId: "TDK001",
            desciption: "Injeksi Obat",
            qty: 1,
            aktifitas: ActivityTindakanIgd.Tindakan,
            ppa: TestDokter(),
            userId: "U1");

        var act = () => tdk.MarkAsVoided(TestAudit(), "Salah input tindakan");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void MarkAsVoided_GivenInvalidReason_ThrowsArgumentException(string? invalidReason)
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        var tdk = TindakanIgdModel.Create(
            visit,
            reffId: "TDK001",
            desciption: "Injeksi Obat",
            qty: 1,
            aktifitas: ActivityTindakanIgd.Tindakan,
            ppa: TestDokter(),
            userId: "U1");

        var act = () => tdk.MarkAsVoided(TestAudit(), invalidReason!);
        act.Should().Throw<ArgumentException>();
    }
}
