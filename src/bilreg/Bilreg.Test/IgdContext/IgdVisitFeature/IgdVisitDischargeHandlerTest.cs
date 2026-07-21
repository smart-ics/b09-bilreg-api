using Bilreg.Application.IgdContext.BedIgdFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature;

public class IgdVisitDischargeHandlerTest
{
    private readonly Mock<IIgdVisitRepo> _visitRepo = new();
    private readonly Mock<IBedIgdRepo> _bedRepo = new();
    private readonly Mock<IPakaiBedIgdRepo> _pakaiRepo = new();
    private readonly IgdVisitDischargeHandler _sut;

    public IgdVisitDischargeHandlerTest()
    {
        _sut = new IgdVisitDischargeHandler(_visitRepo.Object, _bedRepo.Object, _pakaiRepo.Object, TestTglJamProvider.Instance);
    }

    private static IgdVisitModel BuildVisit(
        AdministrativeStateEnum state,
        string bedId = "-",
        bool dischargedAuditSet = false)
    {
        var audit = new AuditInfoType("U1", new DateTime(2026, 1, 1, 8, 0, 0));
        var dischargeAudit = dischargedAuditSet
            ? audit
            : AuditInfoType.Default;
        var reg = state == AdministrativeStateEnum.Daftar
            ? RegModel.Default.ToReff()
            : new RegReff("R001", "P001", "Pasien Tes");
        return new IgdVisitModel(
            igdVisitId: "IGV0001",
            daftarDateTime: audit.Timestamp,
            visitor: VisitorType.Default,
            dokter: PpaType.Default.ToReff(),
            hasTriage: false,
            triage: IgdVisitTriageType.Default,
            administrativeState: state,
            reg: reg,
            redirection: RedirectionType.Default,
            bedId: bedId,
            triageMethod: TriageMethodEnum.Unknown,
            triageColor: TriageColorEnum.Unknown,
            lastTriageAt: new DateTime(3000, 1, 1),
            nextReTriageAt: new DateTime(3000, 1, 1),
            auditTrail: AuditTrailType.Create(audit.UserId, audit.Timestamp),
            dischargeAudit: dischargeAudit,
            listTriage: [],
            listEvent: []);
    }

    [Fact]
    public async Task Handle_AlreadyDischarged_IsIdempotent_NoSaveChangesInvoked()
    {
        var visit = BuildVisit(AdministrativeStateEnum.Discharged, dischargedAuditSet: true);
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        var result = await _sut.Handle(new IgdVisitDischargeCmd("IGV0001", "U1"), CancellationToken.None);

        result.IgdVisitId.Should().Be("IGV0001");
        result.BedReleased.Should().BeFalse();
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
        _bedRepo.Verify(r => r.SaveChanges(It.IsAny<BedIgdModel>()), Times.Never);
        _pakaiRepo.Verify(r => r.SaveChanges(It.IsAny<PakaiBedIgdModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_VisitNotFound_Throws()
    {
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe<IgdVisitModel>.None);

        var act = async () => await _sut.Handle(new IgdVisitDischargeCmd("MISSING", "U1"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_NoBed_DischargesVisitOnly()
    {
        var visit = BuildVisit(AdministrativeStateEnum.Registered);
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        var result = await _sut.Handle(new IgdVisitDischargeCmd("IGV0001", "U1"), CancellationToken.None);

        result.BedReleased.Should().BeFalse();
        result.AdministrativeState.Should().Be(AdministrativeStateEnum.Discharged.ToCode());
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Once);
        _bedRepo.Verify(r => r.SaveChanges(It.IsAny<BedIgdModel>()), Times.Never);
        _pakaiRepo.Verify(r => r.SaveChanges(It.IsAny<PakaiBedIgdModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OnBed_ReleasesBedAndCascades()
    {
        var visit = BuildVisit(AdministrativeStateEnum.Registered, bedId: "B01");
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        var bed = BedIgdModel.CreateMaster("B01", "Bed-01", "Kamar A",
            new AuditInfoType("U0", new DateTime(2026, 1, 1, 7, 0, 0)));
        bed.Occupy(visit.IgdVisitId, new AuditInfoType("U1", new DateTime(2026, 1, 1, 8, 0, 0)));
        _bedRepo
            .Setup(r => r.LoadEntity(It.IsAny<IBedIgdKey>()))
            .Returns(MayBe.From(bed));

        var pakai = PakaiBedIgdModel.Open(visit, bed,
            new AuditInfoType("U1", new DateTime(2026, 1, 1, 8, 0, 0)));
        _pakaiRepo
            .Setup(r => r.LoadOpenForBed(It.IsAny<IBedIgdKey>()))
            .Returns(MayBe.From(pakai));

        var result = await _sut.Handle(new IgdVisitDischargeCmd("IGV0001", "U1"), CancellationToken.None);

        result.BedReleased.Should().BeTrue();
        result.AdministrativeState.Should().Be(AdministrativeStateEnum.Discharged.ToCode());
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Once);
        _bedRepo.Verify(r => r.SaveChanges(It.IsAny<BedIgdModel>()), Times.Once);
        _pakaiRepo.Verify(r => r.SaveChanges(It.IsAny<PakaiBedIgdModel>()), Times.Once);
        bed.IsOccupied.Should().BeFalse();
        pakai.IsOpen.Should().BeFalse();
        visit.HasObserved.Should().BeFalse();
    }
}
