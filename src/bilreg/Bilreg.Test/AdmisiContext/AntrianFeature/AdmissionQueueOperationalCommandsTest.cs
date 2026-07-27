using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionQueueOperationalCommandsTest
{
    private readonly Mock<IAntrianRepo> _queues = new();
    private readonly Mock<IAdmissionQueueOperationRepo> _operations = new();
    private readonly Mock<IAdmissionQueueRefreshPublisher> _publisher = new();
    private readonly Mock<IAuditRepo> _auditRepo = new();

    [Fact]
    public async Task Call_ExplicitEntry_IncrementsThroughCasAndPublishesAfterSuccess()
    {
        SetupQueue(WaitingEntry());
        _operations.Setup(x => x.TryCall("Q", 1, "L1", "u", TestTglJamProvider.Instance.Now)).Returns(true);
        var sut = new AdmissionQueueCallHandler(_queues.Object, _operations.Object,
            TestTglJamProvider.Instance, _publisher.Object);

        var result = await sut.Handle(new AdmissionQueueCallCmd("Q", 1, "L1", "u"), default);

        result.Status.Should().Be("Outstanding");
        _operations.VerifyAll();
        _publisher.Verify(x => x.PublishAsync("L1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartService_WhenCasIsStale_ThrowsConflictAndDoesNotPublish()
    {
        SetupQueue(WaitingEntry());
        _operations.Setup(x => x.TryStartService("Q", 1, "L1", It.IsAny<byte[]>(), "u",
            TestTglJamProvider.Instance.Now)).Returns(false);
        var sut = new AdmissionQueueStartServiceHandler(_queues.Object, _operations.Object,
            TestTglJamProvider.Instance, _publisher.Object);

        var act = () => sut.Handle(new AdmissionQueueStartServiceCmd("Q", 1, "L1", [1], "u"), default);

        await act.Should().ThrowAsync<AdmissionQueueConcurrencyException>();
        _publisher.Verify(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReturnToWaiting_ReleasesClaimAuditsAndPublishesAfterSuccess()
    {
        SetupQueue(WaitingEntry());
        _operations.Setup(x => x.TryReturnToWaiting(
                "Q", 1, "L1", It.IsAny<byte[]>(), "u", TestTglJamProvider.Instance.Now))
            .Returns(true);
        var sut = new AdmissionQueueReturnToWaitingHandler(
            _queues.Object,
            _operations.Object,
            _auditRepo.Object,
            TestTglJamProvider.Instance,
            _publisher.Object);

        var result = await sut.Handle(
            new AdmissionQueueReturnToWaitingCmd("Q", 1, "L1", [1], "u"),
            default);

        result.Status.Should().Be("Waiting");
        _auditRepo.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "RETURN_TO_WAITING" &&
            a.EntityName == "AdmissionQueueLoketClaim" &&
            a.EntityId == "Q:1" &&
            a.Reason == "UnansweredCall" &&
            a.UserId == "u" &&
            a.OriginalDataJson != null &&
            a.OriginalDataJson.Contains("\"loketKey\": \"L1\""))), Times.Once);
        _publisher.Verify(x => x.PublishAsync("L1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReturnToWaiting_WhenCasIsStale_DoesNotAuditOrPublish()
    {
        SetupQueue(WaitingEntry());
        _operations.Setup(x => x.TryReturnToWaiting(
                "Q", 1, "L1", It.IsAny<byte[]>(), "u", TestTglJamProvider.Instance.Now))
            .Returns(false);
        var sut = new AdmissionQueueReturnToWaitingHandler(
            _queues.Object,
            _operations.Object,
            _auditRepo.Object,
            TestTglJamProvider.Instance,
            _publisher.Object);

        var act = () => sut.Handle(
            new AdmissionQueueReturnToWaitingCmd("Q", 1, "L1", [1], "u"),
            default);

        await act.Should().ThrowAsync<AdmissionQueueConcurrencyException>();
        _auditRepo.Verify(
            x => x.SaveChanges(It.IsAny<AuditLog>()),
            Times.Never);
        _publisher.Verify(
            x => x.PublishAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReturnToWaiting_WhenEntryIsNotWaiting_ReturnsConflictBeforeRepository()
    {
        var entry = WaitingEntry();
        entry.Serve(TestTglJamProvider.Instance.Now);
        SetupQueue(entry);
        var sut = new AdmissionQueueReturnToWaitingHandler(
            _queues.Object,
            _operations.Object,
            _auditRepo.Object,
            TestTglJamProvider.Instance,
            _publisher.Object);

        var act = () => sut.Handle(
            new AdmissionQueueReturnToWaitingCmd("Q", 1, "L1", [1], "u"),
            default);

        await act.Should().ThrowAsync<AdmissionQueueConcurrencyException>();
        _operations.Verify(
            x => x.TryReturnToWaiting(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelRegistration_ReturnsInServiceEntryToWaitingAndReleasesClaim()
    {
        var entry = WaitingEntry();
        entry.Serve(TestTglJamProvider.Instance.Now);
        SetupQueue(entry);
        _operations.Setup(x => x.TryCancelRegistration(
                "Q", 1, "L1", It.IsAny<byte[]>(), "u", TestTglJamProvider.Instance.Now))
            .Returns(true);
        var sut = new AdmissionQueueCancelRegistrationHandler(
            _queues.Object,
            _operations.Object,
            _auditRepo.Object,
            TestTglJamProvider.Instance,
            _publisher.Object);

        var result = await sut.Handle(
            new AdmissionQueueCancelRegistrationCmd("Q", 1, "L1", [1], "u"),
            default);

        result.Status.Should().Be("Waiting");
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Waiting);
        _auditRepo.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "CANCEL_REGISTRATION" &&
            a.Reason == "ReturnedToWaiting" &&
            a.EntityId == "Q:1")), Times.Once);
        _publisher.Verify(x => x.PublishAsync("L1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void TransitionMatrix_RejectsOperationalTransitionsFromInServiceAndDone()
    {
        var inService = WaitingEntry(); inService.Serve(TestTglJamProvider.Instance.Now);
        inService.Invoking(x => x.RecordCall()).Should().Throw<InvalidOperationException>();
        inService.Invoking(x => x.Withdraw("reason", "u", TestTglJamProvider.Instance.Now)).Should().Throw<InvalidOperationException>();
        inService.Done(TestTglJamProvider.Instance.Now);
        inService.Invoking(x => x.StartCalledService(TestTglJamProvider.Instance.Now)).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NoShow_IsWithdrawnWithExplicitReasonAndActor()
    {
        var entry = WaitingEntry();
        entry.Withdraw("NoShow", "u", TestTglJamProvider.Instance.Now);
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Withdrawn);
        entry.WithdrawalReason.Should().Be("NoShow");
        entry.WithdrawalUserId.Should().Be("u");
    }

    [Fact]
    public void RedirectReplacement_IsPriorityAndPreservesCompleteSourceIdentity()
    {
        var entry = WaitingEntry(); entry.MarkRedirectReplacement("SOURCE", 7);
        entry.Priority.Should().BeTrue(); entry.CreationReason.Should().Be(1);
        entry.SourceAntrianId.Should().Be("SOURCE"); entry.SourceNoUrut.Should().Be(7);
    }

    private void SetupQueue(AntrianEntryModel entry)
    {
        var queue = new AntrianModel("Q", new DateOnly(2026,7,23), TimeOnly.MinValue,
            TimeOnly.MaxValue, "tag", "Admission", new ServicePointType("ADM","Admission"),
            [entry], Mock.Of<ISequencer>(), "A");
        _queues.Setup(x => x.LoadEntity(It.IsAny<IAntrianKey>())).Returns(MayBe.From(queue));
    }

    private static AntrianEntryModel WaitingEntry() => AntrianEntryModel.Create(1,
        PersonType.Default, PasienTrackerModel.Key("-"), "", "", TestTglJamProvider.Instance.Now);
}
