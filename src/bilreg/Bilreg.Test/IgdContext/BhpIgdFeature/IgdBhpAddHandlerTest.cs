using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.IgdContext.BhpIgdFeature;
using Bilreg.Application.IgdContext.BhpIgdFeature.UseCases;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.IgdContext.BhpIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.BhpIgdFeature;

public class IgdBhpAddHandlerTest
{
    private readonly Mock<IIgdVisitRepo> _visitRepo = new();
    private readonly Mock<IBhpIgdRepo> _bhpRepo = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IIgdBillingPostingOrchestrator> _billingOrchestrator = new();

    private const string IgdVisitId = "IGV-2026-0001";
    private const string RegId = "REG-001";
    private const string BhpItemId = "BHP001";
    private const string BhpItemName = "Kassa Steril";
    private const string UserId = "USER-007";

    private IgdBhpAddHandler CreateSut()
    {
        return new IgdBhpAddHandler(
            _visitRepo.Object,
            _bhpRepo.Object,
            TestTglJamProvider.Instance,
            _regRepo.Object,
            _billingOrchestrator.Object);
    }

    private static RegModel CreateTestReg(string regId = RegId)
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
            PasienModel.Default.ToReff(),
            Domain.AdmisiContext.JaminanFeature.TipeJaminanType.Default.ToReff(),
            Domain.AdmisiContext.JaminanFeature.PolisModel.Default.ToReff(),
            Domain.BedUsageContext.WardFeature.KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            Domain.AdmisiContext.RujukanFeature.RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            new LayananReff("1GD01", "IGD"),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);
    }

    private static IgdVisitModel CreateTestVisit(string igdVisitId = IgdVisitId, bool withReg = false)
    {
        var audit = new AuditInfoType("creator", new DateTime(2026, 1, 1, 8, 0, 0));
        var visitor = new VisitorType("Pasien Tes", "L", new DateOnly(1990, 1, 1), "0812000");
        var visit = new IgdVisitModel(
            igdVisitId: igdVisitId,
            daftarDateTime: audit.Timestamp,
            visitor: visitor,
            dokter: PpaType.Default.ToReff(),
            hasTriage: false,
            triage: IgdVisitTriageType.Default,
            administrativeState: AdministrativeStateEnum.Daftar,
            reg: RegModel.Default.ToReff(),
            redirection: RedirectionType.Default,
            bedId: "-",
            triageMethod: TriageMethodEnum.Unknown,
            triageColor: TriageColorEnum.Unknown,
            lastTriageAt: new DateTime(3000, 1, 1),
            nextReTriageAt: new DateTime(3000, 1, 1),
            auditTrail: AuditTrailType.Create(audit.UserId, audit.Timestamp),
            dischargeAudit: AuditInfoType.Default,
            listTriage: [],
            listEvent: []);
        if (withReg)
        {
            visit.AssignRegister(CreateTestReg(), audit);
        }
        return visit;
    }

    [Fact]
    public async Task Handle_PreRegistration_SavesBhpAsBuffer_WithIsPostedFalse_AndDoesNotPostToBilling()
    {
        // Arrange - visit without registration (pra-registrasi)
        var visit = CreateTestVisit(withReg: false);
        visit.HasReg.Should().BeFalse();

        _visitRepo
            .Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == IgdVisitId)))
            .Returns(MayBe.From(visit));

        var cmd = new IgdBhpAddCmd(
            IgdVisitId, BhpItemId, BhpItemName, 2, 5000m, UserId);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.BhpIgdId.Should().StartWith("BHI");
        result.IgdVisitId.Should().Be(IgdVisitId);
        result.Subtotal.Should().Be(10000m);

        _bhpRepo.Verify(r => r.SaveChanges(It.Is<BhpIgdModel>(b =>
            b.RegId == "-" &&
            !b.IsPosted &&
            b.BhpItemId == BhpItemId &&
            b.Subtotal == 10000m)), Times.Once);
        _visitRepo.Verify(r => r.SaveChanges(visit), Times.Once);
        _billingOrchestrator.Verify(o => o.PostBhpAsync(
            It.IsAny<IgdVisitModel>(), It.IsAny<RegModel>(), It.IsAny<BhpIgdModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PostRegistration_SavesBhp_AndDirectlyPostsToBilling()
    {
        // Arrange - visit with registration (pasca-registrasi)
        var visit = CreateTestVisit(withReg: true);
        var reg = CreateTestReg();
        visit.HasReg.Should().BeTrue();

        _visitRepo
            .Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == IgdVisitId)))
            .Returns(MayBe.From(visit));
        _regRepo
            .Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(reg));

        _billingOrchestrator
            .Setup(o => o.PostBhpAsync(visit, reg, It.IsAny<BhpIgdModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cmd = new IgdBhpAddCmd(
            IgdVisitId, BhpItemId, BhpItemName, 2, 5000m, UserId);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _bhpRepo.Verify(r => r.SaveChanges(It.Is<BhpIgdModel>(b =>
            b.RegId == RegId &&
            b.BhpItemId == BhpItemId)), Times.Once);
        _billingOrchestrator.Verify(o => o.PostBhpAsync(
            visit, reg, It.Is<BhpIgdModel>(b => b.BhpItemId == BhpItemId), It.IsAny<CancellationToken>()), Times.Once);
        visit.HasPendingBilling.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_PostRegistration_WhenBillingPostingFails_SetsHasPendingBillingTrue()
    {
        // Arrange - visit with registration, posting fails
        var visit = CreateTestVisit(withReg: true);
        var reg = CreateTestReg();

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));
        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        _billingOrchestrator
            .Setup(o => o.PostBhpAsync(It.IsAny<IgdVisitModel>(), It.IsAny<RegModel>(), It.IsAny<BhpIgdModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var cmd = new IgdBhpAddCmd(
            IgdVisitId, BhpItemId, BhpItemName, 2, 5000m, UserId);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        visit.HasPendingBilling.Should().BeTrue();
        _visitRepo.Verify(r => r.SaveChanges(It.Is<IgdVisitModel>(v => v.HasPendingBilling)), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_PostRegistration_WhenBillingPostingThrows_CatchesAndSetsHasPendingBillingTrue()
    {
        // Arrange - visit with registration, posting throws
        var visit = CreateTestVisit(withReg: true);
        var reg = CreateTestReg();

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));
        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        _billingOrchestrator
            .Setup(o => o.PostBhpAsync(It.IsAny<IgdVisitModel>(), It.IsAny<RegModel>(), It.IsAny<BhpIgdModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection failed"));

        var cmd = new IgdBhpAddCmd(
            IgdVisitId, BhpItemId, BhpItemName, 2, 5000m, UserId);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert - does not throw to caller, sets pending billing
        await act.Should().NotThrowAsync();
        visit.HasPendingBilling.Should().BeTrue();
        _visitRepo.Verify(r => r.SaveChanges(It.Is<IgdVisitModel>(v => v.HasPendingBilling)), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenVisitNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe<IgdVisitModel>.None);

        var cmd = new IgdBhpAddCmd(
            IgdVisitId, BhpItemId, BhpItemName, 2, 5000m, UserId);
        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{IgdVisitId}*");
    }

    [Theory]
    [InlineData("", "BHP001", 1, 1000, "U1")]
    [InlineData("   ", "BHP001", 1, 1000, "U1")]
    [InlineData("IGV-1", "", 1, 1000, "U1")]
    [InlineData("IGV-1", "   ", 1, 1000, "U1")]
    [InlineData("IGV-1", "BHP001", 1, 1000, "")]
    [InlineData("IGV-1", "BHP001", 1, 1000, "   ")]
    [InlineData("IGV-1", "BHP001", 0, 1000, "U1")]
    [InlineData("IGV-1", "BHP001", -1, 1000, "U1")]
    [InlineData("IGV-1", "BHP001", 1, -100, "U1")]
    public async Task Handle_InvalidParameters_ThrowsArgumentException(
        string visitId, string bhpItemId, int qty, decimal price, string userId)
    {
        var cmd = new IgdBhpAddCmd(visitId, bhpItemId, "Item", qty, price, userId);
        var sut = CreateSut();

        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
