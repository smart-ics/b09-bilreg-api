using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.IgdContext;
using Bilreg.Application.IgdContext.BhpIgdFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Application.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.IgdContext.BhpIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature.UseCases;

public class IgdVisitAssignRegisterHandlerTest
{
    private readonly Mock<IIgdVisitRepo> _visitRepo = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IIgdVisitSmassTaskRepo> _taskRepo = new();
    private readonly Mock<ISmassAssessmentGateway> _smassGateway = new();
    private readonly Mock<IEmrLabelGateway> _emrLabelGateway = new();
    private readonly Mock<ITindakanIgdRepo> _tindakanRepo = new();
    private readonly Mock<IBhpIgdRepo> _bhpRepo = new();
    private readonly Mock<IIgdBillingPostingOrchestrator> _billingOrchestrator = new();

    private readonly IgdVisitOptions _options = new()
    {
        EnableSmassIntegration = true,
        SmassLayananId = "DEFAULT-LAYANAN",
        SmassTriagePaperId = "PP-ICS-TRGE",
        SmassTriagePaperName = "FORMULIR TRIASE IGD"
    };

    private const string IgdVisitId = "IGV-2026-0001";
    private const string RegId = "REG-001";
    private const string UserId = "USER-007";

    public IgdVisitAssignRegisterHandlerTest()
    {
        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, "ASM-01", null, new List<string> { "ASM-01" }));

        _emrLabelGateway
            .Setup(g => g.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmrLabelGatewayResult(true, "OK"));

        _billingOrchestrator
            .Setup(o => o.SyncAllPendingForVisitAsync(It.IsAny<IgdVisitModel>(), It.IsAny<RegModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private IgdVisitAssignRegisterHandler CreateSut(IgdVisitOptions? options = null)
    {
        var opt = Options.Create(options ?? _options);
        return new IgdVisitAssignRegisterHandler(
            _visitRepo.Object,
            _regRepo.Object,
            TestTglJamProvider.Instance,
            _taskRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            opt,
            _tindakanRepo.Object,
            _bhpRepo.Object,
            _billingOrchestrator.Object);
    }

    private static IgdVisitModel CreateTestVisit(string igdVisitId = IgdVisitId)
    {
        var audit = new AuditInfoType("creator", new DateTime(2026, 1, 1, 8, 0, 0));
        var visitor = new VisitorType("Pasien Tes", "L", new DateOnly(1990, 1, 1), "0812000");
        return new IgdVisitModel(
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
    }

    private static RegModel CreateTestReg(
        string regId = RegId,
        string layananId = "1GD01",
        string layananName = "IGD",
        JenisRegEnum jenisReg = JenisRegEnum.Darurat)
    {
        var regDate = new DateOnly(2026, 9, 28);
        return new RegModel(
            regId,
            regDate,
            new AuditInfoType("tester", regDate.ToDateTime(TimeOnly.MinValue)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            jenisReg,
            PasienModel.Default.ToReff(),
            Domain.AdmisiContext.JaminanFeature.TipeJaminanType.Default.ToReff(),
            Domain.AdmisiContext.JaminanFeature.PolisModel.Default.ToReff(),
            Domain.BedUsageContext.WardFeature.KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            Domain.AdmisiContext.RujukanFeature.RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            new LayananReff(layananId, layananName),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);
    }

    [Fact]
    public async Task Handle_ValidRequest_AssignsRegister_AndWiredEmrLabelGatewayWithUserId()
    {
        // Arrange
        var visit = CreateTestVisit();
        var reg = CreateTestReg();
        var cmd = new IgdVisitAssignRegisterCmd(IgdVisitId, RegId, UserId);

        _visitRepo
            .Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == IgdVisitId)))
            .Returns(MayBe.From(visit));

        _regRepo
            .Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(reg));

        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, 0, SmassTaskTypeEnum.Link))
            .Returns(MayBe<IgdVisitSmassTaskModel>.None);

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, "ASM-01", null, new List<string> { "ASM-01", "ASM-02" }));

        _emrLabelGateway
            .Setup(g => g.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmrLabelGatewayResult(true, "Label created"));

        var sut = CreateSut();

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        _visitRepo.Verify(r => r.SaveChanges(It.Is<IgdVisitModel>(v =>
            v.IgdVisitId == IgdVisitId &&
            v.AdministrativeState == AdministrativeStateEnum.Registered &&
            v.Reg.RegId == RegId)), Times.Exactly(2));

        _taskRepo.Verify(r => r.SaveChanges(It.Is<IgdVisitSmassTaskModel>(t =>
            t.IgdVisitId == IgdVisitId &&
            t.TaskStatus == SmassTaskStatusEnum.Succeeded)), Times.Once);

        // Verify EmrLabelGateway was called for both assessments with correct UserId
        _emrLabelGateway.Verify(g => g.AddSmassLabel(It.Is<EmrAddSmassLabelRequest>(req =>
            req.AssesmentId == "ASM-01" &&
            req.RegId == RegId &&
            req.UserrId == UserId &&
            req.PaperId == "PP-ICS-TRGE" &&
            req.PaperName == "FORMULIR TRIASE IGD" &&
            req.LayananId == "1GD01"), It.IsAny<CancellationToken>()), Times.Once);

        _emrLabelGateway.Verify(g => g.AddSmassLabel(It.Is<EmrAddSmassLabelRequest>(req =>
            req.AssesmentId == "ASM-02" &&
            req.RegId == RegId &&
            req.UserrId == UserId &&
            req.PaperId == "PP-ICS-TRGE" &&
            req.PaperName == "FORMULIR TRIASE IGD" &&
            req.LayananId == "1GD01"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmrLabelThrowsException_RegistrationAndSmassTaskSucceed()
    {
        // Arrange
        var visit = CreateTestVisit();
        var reg = CreateTestReg();
        var cmd = new IgdVisitAssignRegisterCmd(IgdVisitId, RegId, UserId);

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, 0, SmassTaskTypeEnum.Link))
            .Returns(MayBe<IgdVisitSmassTaskModel>.None);

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, "ASM-01", null, new List<string> { "ASM-01" }));

        _emrLabelGateway
            .Setup(g => g.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("EMR 2.0 service down"));

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert - should not throw, registration and SMASS task are unaffected
        await act.Should().NotThrowAsync();

        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Exactly(2));
        _taskRepo.Verify(r => r.SaveChanges(It.Is<IgdVisitSmassTaskModel>(t =>
            t.TaskStatus == SmassTaskStatusEnum.Succeeded)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSmassIntegrationDisabled_DoesNotCallGatewayOrEmr()
    {
        // Arrange
        var visit = CreateTestVisit();
        var reg = CreateTestReg();
        var cmd = new IgdVisitAssignRegisterCmd(IgdVisitId, RegId, UserId);

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        var disabledOptions = new IgdVisitOptions
        {
            EnableSmassIntegration = false,
            SmassLayananId = _options.SmassLayananId,
            SmassTriagePaperId = _options.SmassTriagePaperId,
            SmassTriagePaperName = _options.SmassTriagePaperName
        };
        var sut = CreateSut(disabledOptions);

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Exactly(2));
        _smassGateway.Verify(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _emrLabelGateway.Verify(g => g.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenVisitNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var cmd = new IgdVisitAssignRegisterCmd(IgdVisitId, RegId, UserId);

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe<IgdVisitModel>.None);

        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{IgdVisitId}*");
    }

    [Fact]
    public async Task Handle_WhenRegNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var visit = CreateTestVisit();
        var cmd = new IgdVisitAssignRegisterCmd(IgdVisitId, RegId, UserId);

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.None);

        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{RegId}*");
    }

    [Theory]
    [InlineData("", "REG-001", "USER-1")]
    [InlineData("   ", "REG-001", "USER-1")]
    [InlineData("IGV-001", "", "USER-1")]
    [InlineData("IGV-001", "   ", "USER-1")]
    [InlineData("IGV-001", "REG-001", "")]
    [InlineData("IGV-001", "REG-001", "   ")]
    public async Task Handle_EmptyParameters_ThrowsArgumentException(string visitId, string regId, string userId)
    {
        var cmd = new IgdVisitAssignRegisterCmd(visitId, regId, userId);
        var sut = CreateSut();

        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WithPendingTindakanAndBhp_UpdatesRegIdAndSyncsBilling()
    {
        // Arrange
        var visit = CreateTestVisit();
        var reg = CreateTestReg();
        var cmd = new IgdVisitAssignRegisterCmd(IgdVisitId, RegId, UserId);

        _visitRepo
            .Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == IgdVisitId)))
            .Returns(MayBe.From(visit));

        _regRepo
            .Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(reg));

        var tdk = TindakanIgdModel.Create(
            visit, "TDK001", "Injeksi Obat", 1, ActivityTindakanIgd.Tindakan,
            PpaType.Default, UserId, DateTime.Now);
        var tdkView = new TindakanIgdView(
            tdk.TindakanIgdId, visit.IgdVisitId, "-", "TDK001", "Injeksi Obat", 1,
            ActivityTindakanIgd.Tindakan, PpaType.Default.ToReff(), UserId, DateTime.Now);

        var bhp = BhpIgdModel.Create(
            visit, "BHP001", "Kassa Steril", 2, 5000m, new AuditInfoType(UserId, DateTime.Now));
        var bhpView = new BhpIgdView(
            bhp.BhpIgdId, visit.IgdVisitId, "-", "BHP001", "Kassa Steril", 2, 5000m, 10000m, DateTime.Now);

        _tindakanRepo
            .Setup(r => r.ListData(It.Is<IIgdVisitKey>(k => k.IgdVisitId == IgdVisitId)))
            .Returns([tdkView]);
        _tindakanRepo
            .Setup(r => r.LoadEntity(It.Is<ITindakanIgdKey>(k => k.TindakanIgdId == tdk.TindakanIgdId)))
            .Returns(MayBe.From(tdk));

        _bhpRepo
            .Setup(r => r.ListData(It.Is<IIgdVisitKey>(k => k.IgdVisitId == IgdVisitId)))
            .Returns([bhpView]);
        _bhpRepo
            .Setup(r => r.LoadEntity(It.Is<IBhpIgdKey>(k => k.BhpIgdId == bhp.BhpIgdId)))
            .Returns(MayBe.From(bhp));

        _billingOrchestrator
            .Setup(o => o.SyncAllPendingForVisitAsync(visit, reg, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        tdk.RegId.Should().Be(RegId);
        bhp.RegId.Should().Be(RegId);
        _tindakanRepo.Verify(r => r.SaveChanges(It.Is<TindakanIgdModel>(t => t.RegId == RegId)), Times.Once);
        _bhpRepo.Verify(r => r.SaveChanges(It.Is<BhpIgdModel>(b => b.RegId == RegId)), Times.Once);
        _billingOrchestrator.Verify(o => o.SyncAllPendingForVisitAsync(visit, reg, It.IsAny<CancellationToken>()), Times.Once);
        visit.HasPendingBilling.Should().BeFalse();
        _visitRepo.Verify(r => r.SaveChanges(It.Is<IgdVisitModel>(v => !v.HasPendingBilling)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_WhenSyncAllPendingReturnsFalse_SetsHasPendingBillingTrue()
    {
        // Arrange
        var visit = CreateTestVisit();
        var reg = CreateTestReg();
        var cmd = new IgdVisitAssignRegisterCmd(IgdVisitId, RegId, UserId);

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));
        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        _billingOrchestrator
            .Setup(o => o.SyncAllPendingForVisitAsync(It.IsAny<IgdVisitModel>(), It.IsAny<RegModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        visit.HasPendingBilling.Should().BeTrue();
        visit.AdministrativeState.Should().Be(AdministrativeStateEnum.Registered);
        visit.Reg.RegId.Should().Be(RegId);
        _visitRepo.Verify(r => r.SaveChanges(It.Is<IgdVisitModel>(v => v.HasPendingBilling)), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenSyncAllPendingThrows_CatchesAndSetsHasPendingBillingTrue_ResiliencePrinciple()
    {
        // Arrange
        var visit = CreateTestVisit();
        var reg = CreateTestReg();
        var cmd = new IgdVisitAssignRegisterCmd(IgdVisitId, RegId, UserId);

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));
        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        _billingOrchestrator
            .Setup(o => o.SyncAllPendingForVisitAsync(It.IsAny<IgdVisitModel>(), It.IsAny<RegModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Billing network failure"));

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert - resilience principle: does not throw, registration link holds, pending billing flag is set
        await act.Should().NotThrowAsync();
        visit.HasPendingBilling.Should().BeTrue();
        visit.Reg.RegId.Should().Be(RegId);
        _visitRepo.Verify(r => r.SaveChanges(It.Is<IgdVisitModel>(v => v.HasPendingBilling)), Times.Exactly(2));
    }
}
