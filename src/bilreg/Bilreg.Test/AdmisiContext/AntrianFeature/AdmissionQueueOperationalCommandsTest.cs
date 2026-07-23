using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
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
