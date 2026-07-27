using Bilreg.Application.AdmisiContext.AntrianFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public sealed class AdmissionQueueClosingHandlerTest
{
    [Fact]
    public async Task Close_RejectsWithdrawWithoutReason_BeforeRepositoryMutation()
    {
        var repo = new Mock<IAdmissionQueueClosingRepo>(MockBehavior.Strict);
        var sut = new AdmissionQueueCloseHandler(repo.Object, TestTglJamProvider.Instance, new NullAdmissionQueueRefreshPublisher());
        var action = () => sut.Handle(new AdmissionQueueCloseCmd("2026-07-27", "SP", "u", [new("Q", 1, AdmissionQueueClosingDispositions.Withdraw, "", null)]), default);
        await action.Should().ThrowAsync<ArgumentException>();
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Close_RejectsDuplicateDecision_BeforeRepositoryMutation()
    {
        var repo = new Mock<IAdmissionQueueClosingRepo>(MockBehavior.Strict);
        var sut = new AdmissionQueueCloseHandler(repo.Object, TestTglJamProvider.Instance, new NullAdmissionQueueRefreshPublisher());
        var action = () => sut.Handle(new AdmissionQueueCloseCmd("2026-07-27", "SP", "u", [new("Q", 1, AdmissionQueueClosingDispositions.NoShow, null, null), new("Q", 1, AdmissionQueueClosingDispositions.NoShow, null, null)]), default);
        await action.Should().ThrowAsync<ArgumentException>();
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Preview_UsesExactDateAndServicePoint()
    {
        var repo = new Mock<IAdmissionQueueClosingRepo>();
        repo.Setup(x => x.ListWaiting(new DateOnly(2026, 7, 27), "SP")).Returns([]);
        var result = await new AdmissionQueueClosingPreviewHandler(repo.Object).Handle(new("2026-07-27", " SP "), default);
        result.ServicePointId.Should().Be("SP");
        repo.VerifyAll();
    }
}
