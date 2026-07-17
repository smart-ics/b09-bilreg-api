using Bilreg.Application.IgdContext.BedIgdFeature;
using Bilreg.Application.IgdContext.BedIgdFeature.UseCases;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.BedIgdFeature;

public class IgdTransferBedHandlerTest
{
    private readonly Mock<IIgdVisitRepo> _visitRepo = new();
    private readonly Mock<IBedIgdRepo> _bedRepo = new();
    private readonly Mock<IPakaiBedIgdRepo> _pakaiRepo = new();
    private readonly IgdTransferBedHandler _sut;

    public IgdTransferBedHandlerTest()
    {
        _sut = new IgdTransferBedHandler(_visitRepo.Object, _bedRepo.Object, _pakaiRepo.Object);
    }

    private static AuditInfoType Audit() => new("U1", new DateTime(2026, 1, 1, 8, 0, 0));

    private static IgdVisitModel VisitOnBed(string bedId)
    {
        var audit = Audit();
        return new IgdVisitModel(
            igdVisitId: "IGV0001",
            daftarDateTime: audit.Timestamp,
            visitor: new VisitorType("Pasien", "L", new DateOnly(1990, 1, 1), "0812"),
            dokter: PpaType.Default.ToReff(),
            hasTriage: true,
            triage: IgdVisitTriageType.Default,
            administrativeState: AdministrativeStateEnum.Registered,
            reg: new RegReff("R001", "P001", "Pasien"),
            redirection: RedirectionType.Default,
            bedId: bedId,
            triageMethod: TriageMethodEnum.Ats,
            triageColor: TriageColorEnum.Red,
            lastTriageAt: audit.Timestamp,
            nextReTriageAt: new DateTime(3000, 1, 1),
            auditTrail: AuditTrailType.Create(audit.UserId, audit.Timestamp),
            dischargeAudit: AuditInfoType.Default,
            listTriage: [],
            listEvent: []);
    }

    private static BedIgdModel Bed(string id, bool occupied, string? visitId = null)
    {
        var audit = Audit();
        var bed = BedIgdModel.CreateMaster(id, $"Bed-{id}", "K1", audit);
        if (occupied && visitId is not null)
            bed.Occupy(visitId, audit);
        return bed;
    }

    [Fact]
    public async Task Handle_NotObserved_Throws_NoSaves()
    {
        var visit = VisitOnBed("-");
        _visitRepo.Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>())).Returns(MayBe.From(visit));

        var act = async () => await _sut.Handle(
            new IgdTransferBedCmd("IGV0001", "B02", "R", "-", "U1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*tidak sedang menempati bed*");
        _bedRepo.Verify(r => r.SaveChanges(It.IsAny<BedIgdModel>()), Times.Never);
        _pakaiRepo.Verify(r => r.SaveChanges(It.IsAny<PakaiBedIgdModel>()), Times.Never);
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SameBed_Throws_NoSaves()
    {
        var visit = VisitOnBed("B01");
        _visitRepo.Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>())).Returns(MayBe.From(visit));

        var act = async () => await _sut.Handle(
            new IgdTransferBedCmd("IGV0001", "B01", "R", "-", "U1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*sama dengan bed saat ini*");
        _bedRepo.Verify(r => r.SaveChanges(It.IsAny<BedIgdModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TargetNotAvailable_Throws_AfterLoads()
    {
        var visit = VisitOnBed("B01");
        _visitRepo.Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>())).Returns(MayBe.From(visit));

        var sourceBed = Bed("B01", true, visit.IgdVisitId);
        var targetBed = Bed("B02", true, "IGV9999");
        _bedRepo.Setup(r => r.LoadEntity(It.IsAny<IBedIgdKey>()))
            .Returns((IBedIgdKey k) => k.BedIgdId == "B01" ? MayBe.From(sourceBed) : MayBe.From(targetBed));

        var openPakai = PakaiBedIgdModel.Open(visit, sourceBed, Audit());
        _pakaiRepo.Setup(r => r.LoadOpenForBed(It.IsAny<IBedIgdKey>())).Returns(MayBe.From(openPakai));

        var act = async () => await _sut.Handle(
            new IgdTransferBedCmd("IGV0001", "B02", "R", "-", "U1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*tidak tersedia*");
        _bedRepo.Verify(r => r.SaveChanges(It.IsAny<BedIgdModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HappyPath_SavesFiveTimes_AndEmitsTransferBed()
    {
        var visit = VisitOnBed("B01");
        _visitRepo.Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>())).Returns(MayBe.From(visit));

        var sourceBed = Bed("B01", true, visit.IgdVisitId);
        var targetBed = Bed("B02", false);
        _bedRepo.Setup(r => r.LoadEntity(It.IsAny<IBedIgdKey>()))
            .Returns((IBedIgdKey k) => k.BedIgdId == "B01" ? MayBe.From(sourceBed) : MayBe.From(targetBed));

        var openPakai = PakaiBedIgdModel.Open(visit, sourceBed, Audit());
        _pakaiRepo.Setup(r => r.LoadOpenForBed(It.IsAny<IBedIgdKey>())).Returns(MayBe.From(openPakai));

        var result = await _sut.Handle(
            new IgdTransferBedCmd("IGV0001", "B02", "PRIORITY_REALLOCATION", "notes", "U1"), CancellationToken.None);

        result.FromBedIgdId.Should().Be("B01");
        result.ToBedIgdId.Should().Be("B02");
        result.ClosedPakaiBedIgdId.Should().Be(openPakai.PakaiBedIgdId);
        result.NewPakaiBedIgdId.Should().NotBeNullOrWhiteSpace();

        visit.BedId.Should().Be("B02");
        visit.ListEvent.Should().Contain(e => e.EventKind == IgdEventEnum.TransferBed);
        sourceBed.IsOccupied.Should().BeFalse();
        targetBed.CurrentIgdVisitId.Should().Be(visit.IgdVisitId);
        openPakai.IsOpen.Should().BeFalse();

        _bedRepo.Verify(r => r.SaveChanges(It.IsAny<BedIgdModel>()), Times.Exactly(2));
        _pakaiRepo.Verify(r => r.SaveChanges(It.IsAny<PakaiBedIgdModel>()), Times.Exactly(2));
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_VisitSaveThrows_Propagates_AndEarlierSavesWereAttempted()
    {
        var visit = VisitOnBed("B01");
        _visitRepo.Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>())).Returns(MayBe.From(visit));

        var sourceBed = Bed("B01", true, visit.IgdVisitId);
        var targetBed = Bed("B02", false);
        _bedRepo.Setup(r => r.LoadEntity(It.IsAny<IBedIgdKey>()))
            .Returns((IBedIgdKey k) => k.BedIgdId == "B01" ? MayBe.From(sourceBed) : MayBe.From(targetBed));

        var openPakai = PakaiBedIgdModel.Open(visit, sourceBed, Audit());
        _pakaiRepo.Setup(r => r.LoadOpenForBed(It.IsAny<IBedIgdKey>())).Returns(MayBe.From(openPakai));

        _visitRepo.Setup(r => r.SaveChanges(It.IsAny<IgdVisitModel>()))
            .Throws(new InvalidOperationException("persist visit failed"));

        var act = async () => await _sut.Handle(
            new IgdTransferBedCmd("IGV0001", "B02", "R", "-", "U1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("persist visit failed");
        _bedRepo.Verify(r => r.SaveChanges(It.IsAny<BedIgdModel>()), Times.Exactly(2));
        _pakaiRepo.Verify(r => r.SaveChanges(It.IsAny<PakaiBedIgdModel>()), Times.Exactly(2));
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Once);
    }
}
