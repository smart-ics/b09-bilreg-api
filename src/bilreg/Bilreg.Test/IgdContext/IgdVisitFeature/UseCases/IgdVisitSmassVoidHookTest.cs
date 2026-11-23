using Bilreg.Application.IgdContext;
using Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature.UseCases;

public class IgdVisitSmassVoidHookTest
{
    private readonly Mock<IIgdVisitSmassTaskRepo> _taskRepo = new();
    private readonly Mock<ISmassAssessmentGateway> _smassGateway = new();

    private readonly IgdVisitOptions _options = new()
    {
        EnableSmassIntegration = true,
        SmassLayananId = "LAY-IGD",
        SmassTriagePaperId = "PP-ICS-TRGE"
    };

    private const string IgdVisitId = "IGV-2026-0001";
    private const int NoTriage = 1;
    private const string AssessmentId = "ASM-001";

    private static IgdVisitSmassTaskModel CreateSucceededGenerateTask(string assessmentId = AssessmentId)
    {
        var task = IgdVisitSmassTaskModel.CreatePending(IgdVisitId, NoTriage, SmassTaskTypeEnum.Generate);
        task.MarkSucceeded(assessmentId);
        return task;
    }

    [Fact]
    public async Task RunAsync_WhenSmassIntegrationDisabled_ReturnsDisabledWithoutRepoOrGatewayCalls()
    {
        // Arrange
        var disabledOptions = new IgdVisitOptions { EnableSmassIntegration = false };

        // Act
        var result = await IgdVisitSmassVoidHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            disabledOptions,
            IgdVisitId,
            NoTriage,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be("Disabled");
        result.AssessmentId.Should().BeEmpty();
        _taskRepo.Verify(
            r => r.FindByBusinessKey(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SmassTaskTypeEnum>()),
            Times.Never);
        _taskRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitSmassTaskModel>()), Times.Never);
        _smassGateway.Verify(
            g => g.DeleteAssesment(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenGenerateTaskNotFound_ReturnsSkippedWithoutGatewayCall()
    {
        // Arrange
        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, NoTriage, SmassTaskTypeEnum.Generate))
            .Returns(MayBe<IgdVisitSmassTaskModel>.None);

        // Act
        var result = await IgdVisitSmassVoidHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            _options,
            IgdVisitId,
            NoTriage,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be("Skipped");
        result.AssessmentId.Should().BeEmpty();
        _taskRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitSmassTaskModel>()), Times.Never);
        _smassGateway.Verify(
            g => g.DeleteAssesment(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenGenerateTaskHasEmptyAssessmentId_ReturnsSkipped()
    {
        // Arrange
        var failedGenerate = IgdVisitSmassTaskModel.CreatePending(
            IgdVisitId, NoTriage, SmassTaskTypeEnum.Generate);
        failedGenerate.MarkFailed("Timeout on generate");

        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, NoTriage, SmassTaskTypeEnum.Generate))
            .Returns(MayBe.From(failedGenerate));

        // Act
        var result = await IgdVisitSmassVoidHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            _options,
            IgdVisitId,
            NoTriage,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be("Skipped");
        _taskRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitSmassTaskModel>()), Times.Never);
        _smassGateway.Verify(
            g => g.DeleteAssesment(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenVoidTaskAlreadySucceeded_ReturnsVoidedWithoutCallingGateway()
    {
        // Arrange
        var generateTask = CreateSucceededGenerateTask();
        var existingVoid = IgdVisitSmassTaskModel.CreatePending(
            IgdVisitId, NoTriage, SmassTaskTypeEnum.Void, assessmentId: AssessmentId);
        existingVoid.MarkSucceeded(AssessmentId);

        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, NoTriage, SmassTaskTypeEnum.Generate))
            .Returns(MayBe.From(generateTask));
        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, NoTriage, SmassTaskTypeEnum.Void))
            .Returns(MayBe.From(existingVoid));

        // Act
        var result = await IgdVisitSmassVoidHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            _options,
            IgdVisitId,
            NoTriage,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be("Voided");
        result.AssessmentId.Should().Be(AssessmentId);
        _smassGateway.Verify(
            g => g.DeleteAssesment(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _taskRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitSmassTaskModel>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenDeleteSucceeds_UpsertsVoidTaskMarksSucceededAndReturnsVoided()
    {
        // Arrange
        var generateTask = CreateSucceededGenerateTask();
        IgdVisitSmassTaskModel? savedTask = null;

        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, NoTriage, SmassTaskTypeEnum.Generate))
            .Returns(MayBe.From(generateTask));
        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, NoTriage, SmassTaskTypeEnum.Void))
            .Returns(MayBe<IgdVisitSmassTaskModel>.None);
        _taskRepo
            .Setup(r => r.SaveChanges(It.IsAny<IgdVisitSmassTaskModel>()))
            .Callback<IgdVisitSmassTaskModel>(t => savedTask = t);

        _smassGateway
            .Setup(g => g.DeleteAssesment(AssessmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, AssessmentId, null));

        // Act
        var result = await IgdVisitSmassVoidHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            _options,
            IgdVisitId,
            NoTriage,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be("Voided");
        result.AssessmentId.Should().Be(AssessmentId);
        savedTask.Should().NotBeNull();
        savedTask!.TaskType.Should().Be(SmassTaskTypeEnum.Void);
        savedTask.TaskType.ToCode().Should().Be("VOID");
        savedTask.TaskStatus.Should().Be(SmassTaskStatusEnum.Succeeded);
        savedTask.AssessmentId.Should().Be(AssessmentId);
        savedTask.NoTriage.Should().Be(NoTriage);
        _smassGateway.Verify(g => g.DeleteAssesment(AssessmentId, It.IsAny<CancellationToken>()), Times.Once);
        _taskRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitSmassTaskModel>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenDeleteFails_PreservesAssessmentIdMarksFailedAndReturnsFailed()
    {
        // Arrange
        var generateTask = CreateSucceededGenerateTask();
        IgdVisitSmassTaskModel? savedTask = null;

        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, NoTriage, SmassTaskTypeEnum.Generate))
            .Returns(MayBe.From(generateTask));
        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, NoTriage, SmassTaskTypeEnum.Void))
            .Returns(MayBe<IgdVisitSmassTaskModel>.None);
        _taskRepo
            .Setup(r => r.SaveChanges(It.IsAny<IgdVisitSmassTaskModel>()))
            .Callback<IgdVisitSmassTaskModel>(t => savedTask = t);

        _smassGateway
            .Setup(g => g.DeleteAssesment(AssessmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(false, null, "HTTP 503 Service Unavailable"));

        // Act
        var result = await IgdVisitSmassVoidHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            _options,
            IgdVisitId,
            NoTriage,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be("Failed");
        result.AssessmentId.Should().Be(AssessmentId);
        savedTask.Should().NotBeNull();
        savedTask!.TaskStatus.Should().Be(SmassTaskStatusEnum.Failed);
        savedTask.AssessmentId.Should().Be(AssessmentId);
        savedTask.LastError.Should().Contain("503");
        savedTask.RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_WhenGatewayThrows_IsolatesExceptionMarksFailedAndDoesNotThrow()
    {
        // Arrange
        var generateTask = CreateSucceededGenerateTask();
        IgdVisitSmassTaskModel? savedTask = null;

        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, NoTriage, SmassTaskTypeEnum.Generate))
            .Returns(MayBe.From(generateTask));
        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, NoTriage, SmassTaskTypeEnum.Void))
            .Returns(MayBe<IgdVisitSmassTaskModel>.None);
        _taskRepo
            .Setup(r => r.SaveChanges(It.IsAny<IgdVisitSmassTaskModel>()))
            .Callback<IgdVisitSmassTaskModel>(t => savedTask = t);

        _smassGateway
            .Setup(g => g.DeleteAssesment(AssessmentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("SMASS connection refused"));

        // Act
        var result = await IgdVisitSmassVoidHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            _options,
            IgdVisitId,
            NoTriage,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be("Failed");
        savedTask.Should().NotBeNull();
        savedTask!.TaskStatus.Should().Be(SmassTaskStatusEnum.Failed);
        savedTask.AssessmentId.Should().Be(AssessmentId);
        savedTask.LastError.Should().Contain("SMASS connection refused");
    }

    [Fact]
    public async Task RunAsync_WhenSaveChangesThrows_DoesNotThrowAndReturnsPendingWhenReReadEmpty()
    {
        // Arrange
        var generateTask = CreateSucceededGenerateTask();

        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, NoTriage, SmassTaskTypeEnum.Generate))
            .Returns(MayBe.From(generateTask));
        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, NoTriage, SmassTaskTypeEnum.Void))
            .Returns(MayBe<IgdVisitSmassTaskModel>.None);
        _taskRepo
            .Setup(r => r.SaveChanges(It.IsAny<IgdVisitSmassTaskModel>()))
            .Throws(new InvalidOperationException("DB write error"));

        _smassGateway
            .Setup(g => g.DeleteAssesment(AssessmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, AssessmentId, null));

        // Act
        var result = await IgdVisitSmassVoidHook.RunAsync(
            _taskRepo.Object,
            _smassGateway.Object,
            _options,
            IgdVisitId,
            NoTriage,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be("Pending");
        result.AssessmentId.Should().Be(AssessmentId);
    }

    [Fact]
    public void CreatePending_VoidTaskWithZeroNoTriage_ThrowsArgumentException()
    {
        var act = () => IgdVisitSmassTaskModel.CreatePending(
            IgdVisitId, noTriage: 0, SmassTaskTypeEnum.Void);

        act.Should().Throw<ArgumentException>();
    }
}
