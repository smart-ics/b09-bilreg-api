using Bilreg.Application.IgdContext;
using Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature.UseCases;

public class IgdVisitSmassLinkHookTest
{
    private readonly Mock<IIgdVisitSmassTaskRepo> _taskRepo = new();
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
    private const string UserId = "USER-007";

    private static RegModel CreateTestReg(string regId = "REG-001", string layananId = "1GD01", string layananName = "IGD")
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

    [Fact]
    public async Task RunAsync_WhenSmassIntegrationDisabled_DoesNothing()
    {
        // Arrange
        var disabledOptions = new IgdVisitOptions { EnableSmassIntegration = false };
        var reg = CreateTestReg();

        // Act
        await IgdVisitSmassLinkHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            disabledOptions,
            IgdVisitId,
            reg,
            UserId,
            CancellationToken.None);

        // Assert
        _taskRepo.Verify(r => r.FindByBusinessKey(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SmassTaskTypeEnum>()), Times.Never);
        _taskRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitSmassTaskModel>()), Times.Never);
        _smassGateway.Verify(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _emrLabelGateway.Verify(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenLinkSucceedsWithSingleAssessment_InvokesAddSmassLabelWithCorrectPayload()
    {
        // Arrange
        var reg = CreateTestReg(regId: "REG-100", layananId: "1GD01");
        IgdVisitSmassTaskModel? savedTask = null;

        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, 0, SmassTaskTypeEnum.Link))
            .Returns(new MayBe<IgdVisitSmassTaskModel>());

        _taskRepo
            .Setup(r => r.SaveChanges(It.IsAny<IgdVisitSmassTaskModel>()))
            .Callback<IgdVisitSmassTaskModel>(t => savedTask = t);

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(
                Success: true,
                AssessmentId: "ASM-001",
                ErrorMessage: null,
                LinkedAssessmentIds: new[] { "ASM-001" }));

        EmrAddSmassLabelRequest? capturedRequest = null;
        _emrLabelGateway
            .Setup(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()))
            .Callback<EmrAddSmassLabelRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new EmrLabelGatewayResult(true, null));

        // Act
        await IgdVisitSmassLinkHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            _options,
            IgdVisitId,
            reg,
            UserId,
            CancellationToken.None);

        // Assert
        savedTask.Should().NotBeNull();
        savedTask!.TaskStatus.Should().Be(SmassTaskStatusEnum.Succeeded);
        savedTask.AssessmentId.Should().Be("ASM-001");

        _emrLabelGateway.Verify(
            e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.AssesmentId.Should().Be("ASM-001");
        capturedRequest.LayananId.Should().Be("1GD01");
        capturedRequest.PaperId.Should().Be("PP-ICS-TRGE");
        capturedRequest.PaperName.Should().Be("FORMULIR TRIASE IGD");
        capturedRequest.RegId.Should().Be("REG-100");
        capturedRequest.UserrId.Should().Be(UserId);
    }

    [Fact]
    public async Task RunAsync_WhenLinkSucceedsWithMultipleAssessments_InvokesAddSmassLabelForEachAssessmentSequentially()
    {
        // Arrange
        var reg = CreateTestReg(regId: "REG-200", layananId: "1GD01");
        var assessmentIds = new[] { "ASM-001", "ASM-002", "ASM-003" };

        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, 0, SmassTaskTypeEnum.Link))
            .Returns(new MayBe<IgdVisitSmassTaskModel>());

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(
                Success: true,
                AssessmentId: "ASM-001",
                ErrorMessage: null,
                LinkedAssessmentIds: assessmentIds));

        var receivedRequests = new List<EmrAddSmassLabelRequest>();
        _emrLabelGateway
            .Setup(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()))
            .Callback<EmrAddSmassLabelRequest, CancellationToken>((req, _) => receivedRequests.Add(req))
            .ReturnsAsync(new EmrLabelGatewayResult(true, null));

        // Act
        await IgdVisitSmassLinkHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            _options,
            IgdVisitId,
            reg,
            UserId,
            CancellationToken.None);

        // Assert
        _emrLabelGateway.Verify(
            e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        receivedRequests.Should().HaveCount(3);
        receivedRequests[0].AssesmentId.Should().Be("ASM-001");
        receivedRequests[1].AssesmentId.Should().Be("ASM-002");
        receivedRequests[2].AssesmentId.Should().Be("ASM-003");

        foreach (var req in receivedRequests)
        {
            req.LayananId.Should().Be("1GD01");
            req.PaperId.Should().Be("PP-ICS-TRGE");
            req.PaperName.Should().Be("FORMULIR TRIASE IGD");
            req.RegId.Should().Be("REG-200");
            req.UserrId.Should().Be(UserId);
        }
    }

    [Fact]
    public async Task RunAsync_WhenRegLayananIdEmpty_FallsBackToOptionsSmassLayananId()
    {
        // Arrange
        var reg = CreateTestReg(regId: "REG-300", layananId: "");
        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(
                Success: true,
                AssessmentId: "ASM-001",
                ErrorMessage: null,
                LinkedAssessmentIds: new[] { "ASM-001" }));

        EmrAddSmassLabelRequest? capturedRequest = null;
        _emrLabelGateway
            .Setup(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()))
            .Callback<EmrAddSmassLabelRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new EmrLabelGatewayResult(true, null));

        // Act
        await IgdVisitSmassLinkHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            _options,
            IgdVisitId,
            reg,
            UserId,
            CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.LayananId.Should().Be("DEFAULT-LAYANAN");
    }

    [Fact]
    public async Task RunAsync_WhenLinkFails_DoesNotInvokeEmrLabelGateway()
    {
        // Arrange
        var reg = CreateTestReg();
        IgdVisitSmassTaskModel? savedTask = null;

        _taskRepo
            .Setup(r => r.SaveChanges(It.IsAny<IgdVisitSmassTaskModel>()))
            .Callback<IgdVisitSmassTaskModel>(t => savedTask = t);

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(
                Success: false,
                AssessmentId: null,
                ErrorMessage: "Failed to link",
                LinkedAssessmentIds: new[] { "ASM-001" }));

        // Act
        await IgdVisitSmassLinkHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            _options,
            IgdVisitId,
            reg,
            UserId,
            CancellationToken.None);

        // Assert
        savedTask.Should().NotBeNull();
        savedTask!.TaskStatus.Should().Be(SmassTaskStatusEnum.Failed);
        savedTask.LastError.Should().Be("Failed to link");
        _emrLabelGateway.Verify(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenLinkedAssessmentIdsNull_DoesNotInvokeEmrLabelGateway()
    {
        // Arrange
        var reg = CreateTestReg();
        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(
                Success: true,
                AssessmentId: "ASM-001",
                ErrorMessage: null,
                LinkedAssessmentIds: null));

        // Act
        await IgdVisitSmassLinkHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            _options,
            IgdVisitId,
            reg,
            UserId,
            CancellationToken.None);

        // Assert
        _emrLabelGateway.Verify(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenLinkedAssessmentIdsEmpty_DoesNotInvokeEmrLabelGateway()
    {
        // Arrange
        var reg = CreateTestReg();
        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(
                Success: true,
                AssessmentId: "ASM-001",
                ErrorMessage: null,
                LinkedAssessmentIds: Array.Empty<string>()));

        // Act
        await IgdVisitSmassLinkHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            _options,
            IgdVisitId,
            reg,
            UserId,
            CancellationToken.None);

        // Assert
        _emrLabelGateway.Verify(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenEmrLabelGatewayThrows_SwallowsExceptionAndPreservesSmassTaskSucceededStatus()
    {
        // Arrange
        var reg = CreateTestReg();
        IgdVisitSmassTaskModel? savedTask = null;

        _taskRepo
            .Setup(r => r.SaveChanges(It.IsAny<IgdVisitSmassTaskModel>()))
            .Callback<IgdVisitSmassTaskModel>(t => savedTask = t);

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(
                Success: true,
                AssessmentId: "ASM-001",
                ErrorMessage: null,
                LinkedAssessmentIds: new[] { "ASM-001" }));

        _emrLabelGateway
            .Setup(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("EMR 2.0 service down"));

        // Act & Assert (must not throw)
        var act = () => IgdVisitSmassLinkHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            _options,
            IgdVisitId,
            reg,
            UserId,
            CancellationToken.None);

        await act.Should().NotThrowAsync();

        savedTask.Should().NotBeNull();
        savedTask!.TaskStatus.Should().Be(SmassTaskStatusEnum.Succeeded);
        savedTask.AssessmentId.Should().Be("ASM-001");
    }

    [Fact]
    public async Task RunAsync_WhenOneEmrLabelCallFails_ContinuesProcessingRemainingAssessments()
    {
        // Arrange
        var reg = CreateTestReg();
        var assessmentIds = new[] { "ASM-001", "ASM-002", "ASM-003" };

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(
                Success: true,
                AssessmentId: "ASM-001",
                ErrorMessage: null,
                LinkedAssessmentIds: assessmentIds));

        var calledIds = new List<string>();
        _emrLabelGateway
            .Setup(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()))
            .Returns<EmrAddSmassLabelRequest, CancellationToken>((req, _) =>
            {
                calledIds.Add(req.AssesmentId);
                if (req.AssesmentId == "ASM-002")
                    throw new InvalidOperationException("Fail ASM-002");
                return Task.FromResult(new EmrLabelGatewayResult(true, null));
            });

        // Act
        await IgdVisitSmassLinkHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            _options,
            IgdVisitId,
            reg,
            UserId,
            CancellationToken.None);

        // Assert
        calledIds.Should().BeEquivalentTo(new[] { "ASM-001", "ASM-002", "ASM-003" }, options => options.WithStrictOrdering());
        _emrLabelGateway.Verify(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task RunAsync_BackwardsCompatibleOverload_ExecutesLinkWithoutEmrGateway()
    {
        // Arrange
        var reg = CreateTestReg();
        IgdVisitSmassTaskModel? savedTask = null;

        _taskRepo
            .Setup(r => r.SaveChanges(It.IsAny<IgdVisitSmassTaskModel>()))
            .Callback<IgdVisitSmassTaskModel>(t => savedTask = t);

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(
                Success: true,
                AssessmentId: "ASM-001",
                ErrorMessage: null,
                LinkedAssessmentIds: new[] { "ASM-001" }));

        // Act
        await IgdVisitSmassLinkHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            _options,
            IgdVisitId,
            reg,
            CancellationToken.None);

        // Assert
        savedTask.Should().NotBeNull();
        savedTask!.TaskStatus.Should().Be(SmassTaskStatusEnum.Succeeded);
    }
}
