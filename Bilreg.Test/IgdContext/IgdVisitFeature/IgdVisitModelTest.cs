using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature;

public class IgdVisitModelTest
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
    public void Create_GivenValidVisitor_ProducesAggregateInDaftarState()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());

        visit.IgdVisitId.Should().StartWith("IGV");
        visit.AdministrativeState.Should().Be(AdministrativeStateEnum.Daftar);
        visit.HasTriage.Should().BeFalse();
        visit.HasObserved.Should().BeFalse();
        visit.HasReg.Should().BeFalse();
        visit.IsTerminal.Should().BeFalse();
        visit.ListEvent.Should().HaveCount(1);
        visit.ListEvent.First().EventKind.Should().Be(IgdEventEnum.Daftar);
    }

    [Fact]
    public void Create_GivenEmptyVisitorName_Throws()
    {
        var badVisitor = TestVisitor() with { VisitorName = "" };
        var act = () => IgdVisitModel.Create(badVisitor, TestAudit());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssignDokter_GivenDokter_SetsDokterAndEmitsEvent()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        var dokter = TestDokter();

        visit.AssignDokter(dokter, TestAudit());

        visit.Dokter.PpaId.Should().Be(dokter.PpaId);
        visit.ListEvent.Should().Contain(x => x.EventKind == IgdEventEnum.AssignDokter);
    }

    [Fact]
    public void AssessTriage_GivenLevel_SetsHasTriageAndAppendsHistory()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());

        visit.AssessTriage(TriageLevelEnum.Ats2, "TD turun", TestAudit());

        visit.HasTriage.Should().BeTrue();
        visit.Triage.Level.Should().Be(TriageLevelEnum.Ats2);
        visit.ListTriage.Should().HaveCount(1);
        visit.ListTriage.First().NoTriage.Should().Be(1);
        visit.ListEvent.Should().Contain(x => x.EventKind == IgdEventEnum.AssessTriage);
    }

    [Fact]
    public void AssessTriage_TwoTimes_AssignsSequentialNoTriage()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());

        visit.AssessTriage(TriageLevelEnum.Ats3, "-", TestAudit());
        visit.AssessTriage(TriageLevelEnum.Ats2, "-", TestAudit());

        visit.ListTriage.Should().HaveCount(2);
        visit.ListTriage.Select(x => x.NoTriage).Should().ContainInOrder(1, 2);
        visit.Triage.Level.Should().Be(TriageLevelEnum.Ats2);
    }

    [Fact]
    public void AssignBed_BeforeTriage_Throws()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        var act = () => visit.AssignBed("B01", TestAudit());
        act.Should().Throw<InvalidOperationException>().WithMessage("*belum melalui triage*");
    }

    [Fact]
    public void AssignBed_GivenTriageDone_SetsBedAndEmitsEvent()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        visit.AssessTriage(TriageLevelEnum.Ats3, "-", TestAudit());

        visit.AssignBed("B01", TestAudit());

        visit.BedId.Should().Be("B01");
        visit.HasObserved.Should().BeTrue();
        visit.ListEvent.Should().Contain(x => x.EventKind == IgdEventEnum.AssignBed);
    }

    [Fact]
    public void AssignBed_WhenAlreadyOnBed_Throws()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        visit.AssessTriage(TriageLevelEnum.Ats3, "-", TestAudit());
        visit.AssignBed("B01", TestAudit());

        var act = () => visit.AssignBed("B02", TestAudit());

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah menempati bed*");
    }

    [Fact]
    public void CheckOutBed_FromOccupied_ClearsBed()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        visit.AssessTriage(TriageLevelEnum.Ats3, "-", TestAudit());
        visit.AssignBed("B01", TestAudit());

        visit.CheckOutBed(TestAudit());

        visit.BedId.Should().Be("-");
        visit.HasObserved.Should().BeFalse();
        visit.ListEvent.Should().Contain(x => x.EventKind == IgdEventEnum.CheckOut);
    }

    [Fact]
    public void Discharge_WithoutReg_Throws()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        var act = () => visit.Discharge(TestAudit());
        act.Should().Throw<InvalidOperationException>().WithMessage("*belum memiliki registrasi*");
    }

    [Fact]
    public void Discharge_WhenVoided_Throws()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        visit.Void(false, false, TestAudit());

        var act = () => visit.Discharge(TestAudit());
        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah di-void*");
    }

    [Fact]
    public void Void_WithExistingTindakan_Throws()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        var act = () => visit.Void(hasTindakan: true, hasBhp: false, audit: TestAudit());
        act.Should().Throw<InvalidOperationException>().WithMessage("*tindakan*");
    }

    [Fact]
    public void Void_WithExistingBhp_Throws()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        var act = () => visit.Void(hasTindakan: false, hasBhp: true, audit: TestAudit());
        act.Should().Throw<InvalidOperationException>().WithMessage("*BHP*");
    }

    [Fact]
    public void Void_WhenClean_MarksVoidedAndIdempotent()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());

        visit.Void(false, false, TestAudit());
        visit.IsVoided.Should().BeTrue();

        var act = () => visit.Void(false, false, TestAudit());
        act.Should().NotThrow();
    }

    [Fact]
    public void RedirectToRawatJalan_WhenOnBed_Throws()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        visit.AssessTriage(TriageLevelEnum.Ats3, "-", TestAudit());
        visit.AssignBed("B01", TestAudit());

        var act = () => visit.RedirectToRawatJalan("RDR1", "Dipulangkan rawat jalan", TestAudit());

        act.Should().Throw<InvalidOperationException>().WithMessage("*menempati bed*");
    }

    [Fact]
    public void RedirectToRawatJalan_WhenFree_TransitionsToRedirected()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        visit.AssessTriage(TriageLevelEnum.Ats3, "-", TestAudit());

        visit.RedirectToRawatJalan("RDR1", "Rajal", TestAudit());

        visit.AdministrativeState.Should().Be(AdministrativeStateEnum.Redirected);
        visit.Redirection.RedirectRajalId.Should().Be("RDR1");
    }

    [Fact]
    public void ClearBed_OnCascade_ResetsBedAndEmitsEvent()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        visit.AssessTriage(TriageLevelEnum.Ats3, "-", TestAudit());
        visit.AssignBed("B01", TestAudit());

        visit.ClearBed(TestAudit());

        visit.BedId.Should().Be("-");
        visit.ListEvent.Should().Contain(x => x.EventKind == IgdEventEnum.ClearBed);
    }
}
