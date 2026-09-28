using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.IgdContext;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature.UseCases;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitSmassTaskFeature.UseCases;

public class IgdVisitSmassTaskRetryExecutorTest
{
    private readonly Mock<IIgdVisitSmassTaskRepo> _taskRepo = new();
    private readonly Mock<IIgdVisitRepo> _visitRepo = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<ISmassAssessmentGateway> _smassGateway = new();
    private readonly Mock<IEmrLabelGateway> _emrLabelGateway = new();

    private readonly IgdVisitOptions _options = new()
    {
        EnableSmassIntegration = true,
        SmassLayananId = "DEFAULT-LAYANAN",
        SmassTriagePaperId = "PP-ICS-TRGE",
        SmassTriagePaperName = "FORMULIR TRIASE IGD"
    };

    private const string IgdVisitId = "IGV-2026-0001";
    private const string RegId = "REG-001";
    private const string AuditUserId = "AUDIT-USER-99";
    private const string OperatorUserId = "OPERATOR-01";

    private static IgdVisitModel CreateTestVisit(string igdVisitId = IgdVisitId, bool hasReg = true, bool hasTriage = false)
    {
        var audit = new AuditInfoType("creator", new DateTime(2026, 1, 1, 8, 0, 0));
        var visitor = new VisitorType("Pasien Tes", "L", new DateOnly(1990, 1, 1), "0812000");
        var regReff = hasReg ? new RegReff(RegId, "PAS-001", "Pasien Tes") : RegModel.Default.ToReff();

        var listTriage = hasTriage
            ? new List<IgdVisitTriageType> { IgdVisitTriageType.Default with { NoTriage = 1 } }
            : new List<IgdVisitTriageType>();

        return new IgdVisitModel(
            igdVisitId: igdVisitId,
            daftarDateTime: audit.Timestamp,
            visitor: visitor,
            dokter: PpaType.Default.ToReff(),
            hasTriage: hasTriage,
            triage: hasTriage ? listTriage[0] : IgdVisitTriageType.Default,
            administrativeState: AdministrativeStateEnum.Daftar,
            reg: regReff,
            redirection: RedirectionType.Default,
            bedId: "-",
            triageMethod: TriageMethodEnum.Unknown,
            triageColor: TriageColorEnum.Unknown,
            lastTriageAt: new DateTime(3000, 1, 1),
            nextReTriageAt: new DateTime(3000, 1, 1),
            auditTrail: AuditTrailType.Create(audit.UserId, audit.Timestamp),
            dischargeAudit: AuditInfoType.Default,
            listTriage: listTriage,
            listEvent: []);
    }

    private static RegModel CreateTestReg(
        string regId = RegId,
        string layananId = "1GD01",
        string layananName = "IGD",
        string userId = AuditUserId)
    {
        var regDate = new DateOnly(2026, 9, 28);
        return new RegModel(
            regId,
            regDate,
            new AuditInfoType(userId, regDate.ToDateTime(TimeOnly.MinValue)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.Darurat,
            PasienModel.Default.ToReff(),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            new LayananReff(layananId, layananName),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);
    }

    private static IgdVisitSmassTaskModel CreateFailedTask(
        SmassTaskTypeEnum taskType = SmassTaskTypeEnum.Link,
        int noTriage = 0)
    {
        var task = IgdVisitSmassTaskModel.CreatePending(IgdVisitId, noTriage, taskType);
        task.MarkFailed("Previous failure");
        return task;
    }

    [Fact]
    public async Task ExecuteAsync_WhenLinkTaskSucceedsWithAssessmentIds_InvokesAddSmassLabelForEachAssessment()
    {
        // Arrange
        var task = CreateFailedTask(SmassTaskTypeEnum.Link, noTriage: 0);
        var visit = CreateTestVisit();
        var reg = CreateTestReg();
        var assessmentIds = new[] { "ASSESS-01", "ASSESS-02" };

        _visitRepo
            .Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == IgdVisitId)))
            .Returns(MayBe.From(visit));

        _regRepo
            .Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(reg));

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, "ASSESS-01", null, assessmentIds));

        var recordedLabelRequests = new List<EmrAddSmassLabelRequest>();
        _emrLabelGateway
            .Setup(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()))
            .Callback<EmrAddSmassLabelRequest, CancellationToken>((req, _) => recordedLabelRequests.Add(req))
            .ReturnsAsync(new EmrLabelGatewayResult(true, null));

        // Act
        await IgdVisitSmassTaskRetryExecutor.ExecuteAsync(
            _taskRepo.Object,
            _visitRepo.Object,
            _regRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            _options,
            task,
            OperatorUserId,
            CancellationToken.None);

        // Assert
        task.TaskStatus.Should().Be(SmassTaskStatusEnum.Succeeded);
        _taskRepo.Verify(r => r.SaveChanges(task), Times.Once);

        recordedLabelRequests.Should().HaveCount(2);
        recordedLabelRequests[0].AssesmentId.Should().Be("ASSESS-01");
        recordedLabelRequests[0].LayananId.Should().Be("1GD01");
        recordedLabelRequests[0].PaperId.Should().Be("PP-ICS-TRGE");
        recordedLabelRequests[0].PaperName.Should().Be("FORMULIR TRIASE IGD");
        recordedLabelRequests[0].RegId.Should().Be(RegId);
        recordedLabelRequests[0].UserrId.Should().Be(OperatorUserId);

        recordedLabelRequests[1].AssesmentId.Should().Be("ASSESS-02");
        recordedLabelRequests[1].LayananId.Should().Be("1GD01");
        recordedLabelRequests[1].PaperId.Should().Be("PP-ICS-TRGE");
        recordedLabelRequests[1].PaperName.Should().Be("FORMULIR TRIASE IGD");
        recordedLabelRequests[1].RegId.Should().Be(RegId);
        recordedLabelRequests[1].UserrId.Should().Be(OperatorUserId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIdIsEmpty_FallsBackToRegAuditUserId()
    {
        // Arrange
        var task = CreateFailedTask(SmassTaskTypeEnum.Link, noTriage: 0);
        var visit = CreateTestVisit();
        var reg = CreateTestReg(userId: AuditUserId);
        var assessmentIds = new[] { "ASSESS-FALLBACK" };

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, "ASSESS-FALLBACK", null, assessmentIds));

        EmrAddSmassLabelRequest? capturedRequest = null;
        _emrLabelGateway
            .Setup(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()))
            .Callback<EmrAddSmassLabelRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new EmrLabelGatewayResult(true, null));

        // Act - pass empty string for userId
        await IgdVisitSmassTaskRetryExecutor.ExecuteAsync(
            _taskRepo.Object,
            _visitRepo.Object,
            _regRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            _options,
            task,
            "",
            CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.UserrId.Should().Be(AuditUserId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRegLayananIdIsEmpty_FallsBackToOptionsSmassLayananId()
    {
        // Arrange
        var task = CreateFailedTask(SmassTaskTypeEnum.Link, noTriage: 0);
        var visit = CreateTestVisit();
        var reg = CreateTestReg(layananId: "");
        var assessmentIds = new[] { "ASSESS-LAYANAN" };

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, "ASSESS-LAYANAN", null, assessmentIds));

        EmrAddSmassLabelRequest? capturedRequest = null;
        _emrLabelGateway
            .Setup(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()))
            .Callback<EmrAddSmassLabelRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new EmrLabelGatewayResult(true, null));

        // Act
        await IgdVisitSmassTaskRetryExecutor.ExecuteAsync(
            _taskRepo.Object,
            _visitRepo.Object,
            _regRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            _options,
            task,
            OperatorUserId,
            CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.LayananId.Should().Be(_options.SmassLayananId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmrLabelThrows_DoesNotThrowAndTaskRemainsSucceeded()
    {
        // Arrange
        var task = CreateFailedTask(SmassTaskTypeEnum.Link, noTriage: 0);
        var visit = CreateTestVisit();
        var reg = CreateTestReg();
        var assessmentIds = new[] { "ASSESS-EX" };

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, "ASSESS-EX", null, assessmentIds));

        _emrLabelGateway
            .Setup(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("EMR 2.0 endpoint down"));

        // Act
        var act = () => IgdVisitSmassTaskRetryExecutor.ExecuteAsync(
            _taskRepo.Object,
            _visitRepo.Object,
            _regRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            _options,
            task,
            OperatorUserId,
            CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        task.TaskStatus.Should().Be(SmassTaskStatusEnum.Succeeded);
        _taskRepo.Verify(r => r.SaveChanges(task), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLinkTaskSucceedsWithNullOrEmptyLinkedAssessmentIds_DoesNotInvokeEmrGateway()
    {
        // Arrange
        var task = CreateFailedTask(SmassTaskTypeEnum.Link, noTriage: 0);
        var visit = CreateTestVisit();
        var reg = CreateTestReg();

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, null, null, Array.Empty<string>()));

        // Act
        await IgdVisitSmassTaskRetryExecutor.ExecuteAsync(
            _taskRepo.Object,
            _visitRepo.Object,
            _regRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            _options,
            task,
            OperatorUserId,
            CancellationToken.None);

        // Assert
        task.TaskStatus.Should().Be(SmassTaskStatusEnum.Succeeded);
        _emrLabelGateway.Verify(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLinkTaskFails_MarksTaskFailedAndDoesNotInvokeEmrGateway()
    {
        // Arrange
        var task = CreateFailedTask(SmassTaskTypeEnum.Link, noTriage: 0);
        var visit = CreateTestVisit();
        var reg = CreateTestReg();

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(false, null, "Link connection timed out"));

        // Act
        await IgdVisitSmassTaskRetryExecutor.ExecuteAsync(
            _taskRepo.Object,
            _visitRepo.Object,
            _regRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            _options,
            task,
            OperatorUserId,
            CancellationToken.None);

        // Assert
        task.TaskStatus.Should().Be(SmassTaskStatusEnum.Failed);
        task.LastError.Should().Contain("Link connection timed out");
        _emrLabelGateway.Verify(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _taskRepo.Verify(r => r.SaveChanges(task), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenGenerateTaskSucceeds_InvokesGenerateGatewayAndDoesNotInvokeEmrGateway()
    {
        // Arrange
        var task = CreateFailedTask(SmassTaskTypeEnum.Generate, noTriage: 1);
        var visit = CreateTestVisit(hasTriage: true);

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        _smassGateway
            .Setup(g => g.GenerateIgdTriage(It.IsAny<SmassGenerateIgdTriageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, "ASSESS-GEN-1", null));

        // Act
        await IgdVisitSmassTaskRetryExecutor.ExecuteAsync(
            _taskRepo.Object,
            _visitRepo.Object,
            _regRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            _options,
            task,
            OperatorUserId,
            CancellationToken.None);

        // Assert
        task.TaskStatus.Should().Be(SmassTaskStatusEnum.Succeeded);
        _smassGateway.Verify(g => g.GenerateIgdTriage(It.IsAny<SmassGenerateIgdTriageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _emrLabelGateway.Verify(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _taskRepo.Verify(r => r.SaveChanges(task), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_BackwardsCompatibleOverload_ExecutesSuccessfullyWithoutEmrGateway()
    {
        // Arrange
        var task = CreateFailedTask(SmassTaskTypeEnum.Link, noTriage: 0);
        var visit = CreateTestVisit();
        var reg = CreateTestReg();

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, "ASSESS-BC", null, new[] { "ASSESS-BC" }));

        // Act - call overload without IEmrLabelGateway / userId
        await IgdVisitSmassTaskRetryExecutor.ExecuteAsync(
            _taskRepo.Object,
            _visitRepo.Object,
            _regRepo.Object,
            _smassGateway.Object,
            _options,
            task,
            CancellationToken.None);

        // Assert
        task.TaskStatus.Should().Be(SmassTaskStatusEnum.Succeeded);
        _taskRepo.Verify(r => r.SaveChanges(task), Times.Once);
    }
}
