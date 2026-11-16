using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature.UseCases;

public class IgdVisitRetryBillingPostingHandlerTest
{
    private readonly Mock<IIgdVisitRepo> _visitRepo = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IIgdBillingPostingOrchestrator> _billingOrchestrator = new();
    private readonly IgdVisitRetryBillingPostingHandler _sut;

    public IgdVisitRetryBillingPostingHandlerTest()
    {
        _sut = new IgdVisitRetryBillingPostingHandler(
            _visitRepo.Object,
            _regRepo.Object,
            _billingOrchestrator.Object);
    }

    private static IgdVisitModel BuildVisit(bool hasReg = true, bool hasPendingBilling = true)
    {
        var audit = new AuditInfoType("U1", new DateTime(2026, 1, 1, 8, 0, 0));
        var reg = hasReg
            ? new RegReff("REG001", "PAS001", "Pasien Tes")
            : RegModel.Default.ToReff();

        return new IgdVisitModel(
            igdVisitId: "IGV0001",
            daftarDateTime: audit.Timestamp,
            visitor: VisitorType.Default,
            dokter: PpaType.Default.ToReff(),
            hasTriage: false,
            triage: IgdVisitTriageType.Default,
            administrativeState: AdministrativeStateEnum.Registered,
            reg: reg,
            redirection: RedirectionType.Default,
            bedId: "-",
            triageMethod: TriageMethodEnum.Unknown,
            triageColor: TriageColorEnum.Unknown,
            lastTriageAt: new DateTime(3000, 1, 1),
            nextReTriageAt: new DateTime(3000, 1, 1),
            auditTrail: AuditTrailType.Create(audit.UserId, audit.Timestamp),
            dischargeAudit: AuditInfoType.Default,
            listTriage: [],
            listEvent: [],
            hasPendingBilling: hasPendingBilling);
    }

    private static RegModel BuildReg(string regId = "REG001")
    {
        var regDate = new DateOnly(2026, 9, 28);
        return new RegModel(
            regId,
            regDate,
            new AuditInfoType("tester", regDate.ToDateTime(TimeOnly.MinValue)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.Darurat,
            Bilreg.Domain.PasienContext.PasienFeature.PasienModel.Default.ToReff(),
            Bilreg.Domain.AdmisiContext.JaminanFeature.TipeJaminanType.Default.ToReff(),
            Bilreg.Domain.AdmisiContext.JaminanFeature.PolisModel.Default.ToReff(),
            Bilreg.Domain.BedUsageContext.WardFeature.KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            Bilreg.Domain.AdmisiContext.RujukanFeature.RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            new LayananReff("1GD01", "IGD"),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_GivenInvalidVisitId_ThrowsArgumentException(string? invalidVisitId)
    {
        var act = async () => await _sut.Handle(new IgdVisitRetryBillingPostingCmd(invalidVisitId!), CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_VisitNotFound_ThrowsKeyNotFoundException()
    {
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe<IgdVisitModel>.None);

        var act = async () => await _sut.Handle(new IgdVisitRetryBillingPostingCmd("NOTFOUND"), CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_VisitHasNoReg_ThrowsInvalidOperationException()
    {
        var visit = BuildVisit(hasReg: false);
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        var act = async () => await _sut.Handle(new IgdVisitRetryBillingPostingCmd(visit.IgdVisitId), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*belum memiliki registrasi administratif*");
    }

    [Fact]
    public async Task Handle_RegNotFound_ThrowsKeyNotFoundException()
    {
        var visit = BuildVisit(hasReg: true);
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));
        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.None);

        var act = async () => await _sut.Handle(new IgdVisitRetryBillingPostingCmd(visit.IgdVisitId), CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Reg 'REG001' not found*");
    }

    [Fact]
    public async Task Handle_SyncAllPendingSuccess_UpdatesVisitPendingBillingFalse_AndReturnsSuccess()
    {
        var visit = BuildVisit(hasReg: true, hasPendingBilling: true);
        var reg = BuildReg("REG001");

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));
        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));
        _billingOrchestrator
            .Setup(b => b.SyncAllPendingForVisitAsync(visit, reg, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = await _sut.Handle(new IgdVisitRetryBillingPostingCmd(visit.IgdVisitId), CancellationToken.None);

        response.IgdVisitId.Should().Be(visit.IgdVisitId);
        response.AllSuccess.Should().BeTrue();
        response.HasPendingBilling.Should().BeFalse();
        visit.HasPendingBilling.Should().BeFalse();
        _visitRepo.Verify(r => r.SaveChanges(visit), Times.Once);
    }

    [Fact]
    public async Task Handle_SyncAllPendingFails_UpdatesVisitPendingBillingTrue_AndReturnsFalse()
    {
        var visit = BuildVisit(hasReg: true, hasPendingBilling: false);
        var reg = BuildReg("REG001");

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));
        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));
        _billingOrchestrator
            .Setup(b => b.SyncAllPendingForVisitAsync(visit, reg, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var response = await _sut.Handle(new IgdVisitRetryBillingPostingCmd(visit.IgdVisitId), CancellationToken.None);

        response.IgdVisitId.Should().Be(visit.IgdVisitId);
        response.AllSuccess.Should().BeFalse();
        response.HasPendingBilling.Should().BeTrue();
        visit.HasPendingBilling.Should().BeTrue();
        _visitRepo.Verify(r => r.SaveChanges(visit), Times.Once);
    }

    [Fact]
    public async Task Handle_SyncAllPendingThrowsException_ResilientlySetsPendingBillingTrue_AndReturnsFalse()
    {
        var visit = BuildVisit(hasReg: true, hasPendingBilling: false);
        var reg = BuildReg("REG001");

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));
        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));
        _billingOrchestrator
            .Setup(b => b.SyncAllPendingForVisitAsync(visit, reg, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Billing engine failure"));

        var response = await _sut.Handle(new IgdVisitRetryBillingPostingCmd(visit.IgdVisitId), CancellationToken.None);

        response.IgdVisitId.Should().Be(visit.IgdVisitId);
        response.AllSuccess.Should().BeFalse();
        response.HasPendingBilling.Should().BeTrue();
        visit.HasPendingBilling.Should().BeTrue();
        _visitRepo.Verify(r => r.SaveChanges(visit), Times.Once);
    }
}
