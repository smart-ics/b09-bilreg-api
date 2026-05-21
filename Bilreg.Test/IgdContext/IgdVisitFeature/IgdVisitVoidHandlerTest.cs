using Bilreg.Application.IgdContext.BedIgdFeature;
using Bilreg.Application.IgdContext.BhpIgdFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;
using Bilreg.Application.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.Shared.AuditLogFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature;

public class IgdVisitVoidHandlerTest
{
    private readonly Mock<IIgdVisitRepo> _visitRepo = new();
    private readonly Mock<IBedIgdRepo> _bedRepo = new();
    private readonly Mock<IPakaiBedRepo> _pakaiRepo = new();
    private readonly Mock<ITindakanIgdRepo> _tindakanRepo = new();
    private readonly Mock<IBhpIgdRepo> _bhpRepo = new();
    private readonly Mock<AuditLogRepo> _auditLogRepo = new();
    private readonly IgdVisitVoidHandler _sut;

    public IgdVisitVoidHandlerTest()
    {
        _sut = new IgdVisitVoidHandler(
            _visitRepo.Object,
            _bedRepo.Object,
            _pakaiRepo.Object,
            _tindakanRepo.Object,
            _bhpRepo.Object,
            _auditLogRepo.Object);
    }

    private static IgdVisitModel BuildVisit(string bedId = "-", bool voided = false)
    {
        var audit = new AuditInfoType("U1", new DateTime(2026, 1, 1, 8, 0, 0));
        var auditTrail = AuditTrailType.Create(audit.UserId, audit.Timestamp);
        if (voided) auditTrail.Batal(audit.UserId, audit.Timestamp);

        return new IgdVisitModel(
            igdVisitId: "IGV0001",
            daftarDateTime: audit.Timestamp,
            visitor: VisitorType.Default,
            dokter: PpaType.Default.ToReff(),
            hasTriage: false,
            triage: IgdVisitTriageType.Default,
            administrativeState: AdministrativeStateEnum.Daftar,
            reg: RegModel.Default.ToReff(),
            redirection: RedirectionType.Default,
            bedId: bedId,
            triageMethod: TriageMethodEnum.Unknown,
            triageColor: TriageColorEnum.Unknown,
            lastTriageAt: new DateTime(3000, 1, 1),
            nextReTriageAt: new DateTime(3000, 1, 1),
            auditTrail: auditTrail,
            dischargeAudit: AuditInfoType.Default,
            listTriage: [],
            listEvent: []);
    }

    [Fact]
    public async Task Handle_AlreadyVoided_IsIdempotent_NoSaveChangesInvoked()
    {
        var visit = BuildVisit(voided: true);
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        var result = await _sut.Handle(new IgdVisitVoidCmd("IGV0001", "U1", "Reason", "A", "B"), CancellationToken.None);

        result.IsVoided.Should().BeTrue();
        result.BedReleased.Should().BeFalse();
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
        _tindakanRepo.Verify(r => r.AnyForVisit(It.IsAny<IIgdVisitKey>()), Times.Never);
        _bhpRepo.Verify(r => r.AnyForVisit(It.IsAny<IIgdVisitKey>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HasTindakan_Throws()
    {
        var visit = BuildVisit();
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));
        _tindakanRepo.Setup(r => r.AnyForVisit(It.IsAny<IIgdVisitKey>())).Returns(true);
        _bhpRepo.Setup(r => r.AnyForVisit(It.IsAny<IIgdVisitKey>())).Returns(false);

        var act = async () => await _sut.Handle(new IgdVisitVoidCmd("IGV0001", "U1", "Reason", "A", "B"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*tindakan*");
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HasBhp_Throws()
    {
        var visit = BuildVisit();
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));
        _tindakanRepo.Setup(r => r.AnyForVisit(It.IsAny<IIgdVisitKey>())).Returns(false);
        _bhpRepo.Setup(r => r.AnyForVisit(It.IsAny<IIgdVisitKey>())).Returns(true);

        var act = async () => await _sut.Handle(new IgdVisitVoidCmd("IGV0001", "U1", "Reason", "A", "B"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*BHP*");
    }

    [Fact]
    public async Task Handle_Clean_Voids_AndCascadesBedRelease()
    {
        var visit = BuildVisit(bedId: "B01");
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        var bed = BedIgdModel.CreateMaster("B01", "Bed-01", "Kamar A",
            new AuditInfoType("U0", new DateTime(2026, 1, 1, 7, 0, 0)));
        bed.Occupy(visit.IgdVisitId, new AuditInfoType("U1", new DateTime(2026, 1, 1, 8, 0, 0)));
        _bedRepo
            .Setup(r => r.LoadEntity(It.IsAny<IBedIgdKey>()))
            .Returns(MayBe.From(bed));

        var pakai = PakaiBedModel.Open(visit, bed,
            new AuditInfoType("U1", new DateTime(2026, 1, 1, 8, 0, 0)));
        _pakaiRepo
            .Setup(r => r.LoadOpenForBed(It.IsAny<IBedIgdKey>()))
            .Returns(MayBe.From(pakai));

        _tindakanRepo.Setup(r => r.AnyForVisit(It.IsAny<IIgdVisitKey>())).Returns(false);
        _bhpRepo.Setup(r => r.AnyForVisit(It.IsAny<IIgdVisitKey>())).Returns(false);

        var result = await _sut.Handle(new IgdVisitVoidCmd("IGV0001", "U1", "Reason", "A", "B"), CancellationToken.None);

        result.IsVoided.Should().BeTrue();
        result.BedReleased.Should().BeTrue();
        _bedRepo.Verify(r => r.SaveChanges(It.IsAny<BedIgdModel>()), Times.Once);
        _pakaiRepo.Verify(r => r.SaveChanges(It.IsAny<PakaiBedModel>()), Times.Once);
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Once);
        bed.IsOccupied.Should().BeFalse();
        pakai.IsOpen.Should().BeFalse();
    }
}
