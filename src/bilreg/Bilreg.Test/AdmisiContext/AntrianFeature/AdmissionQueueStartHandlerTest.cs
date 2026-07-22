using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionQueueStartHandlerTest
{
    [Fact]
    public async Task Start_WhenAnonymousWaiting_ThenServesQueueWithoutTrackerPersistence()
    {
        var sequencer = new Mock<ISequencer>();
        sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(1);
        var createdAt = new DateTime(2025, 5, 3, 8, 0, 0);
        var queue = new AntrianModel(
            "ADM-Q1", DateOnly.FromDateTime(createdAt), TimeOnly.MinValue, TimeOnly.MaxValue,
            "tag", "Loket", new ServicePointType("ADM", "Loket Admisi"), [], sequencer.Object);
        queue.AddEntry(createdAt);
        var repo = new Mock<IAntrianRepo>();
        repo.Setup(x => x.LoadEntity(It.IsAny<IAntrianKey>())).Returns(MayBe.From(queue));
        repo.Setup(x => x.TrySaveWaitingToInServiceTransition(
                It.IsAny<AntrianModel>(), It.IsAny<AntrianEntryModel>()))
            .Returns(true);

        var resolver = new AdmissionServicePointResolver(Options.Create(new AdmisiRajalOptions()));
        var sut = new AdmissionQueueStartHandler(
            repo.Object, TestTglJamProvider.Instance, resolver);
        var result = await sut.Handle(
            new AdmissionQueueStartCmd("ADM-Q1", 1, "user1"), CancellationToken.None);

        result.Status.Should().Be("InService");
        result.ServedAt.Should().Be(TestTglJamProvider.Instance.Now);
        var entry = queue.ListEntry.Single();
        entry.Tracker.PasienTrackerId.Should().Be("-");
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.InService);
        repo.Verify(x => x.TrySaveWaitingToInServiceTransition(queue, entry), Times.Once);
        repo.Verify(x => x.SaveChanges(It.IsAny<AntrianModel>()), Times.Never);
    }

    [Fact]
    public async Task Start_WhenCompareAndSetFails_ThenThrowsConcurrencyException()
    {
        var sequencer = new Mock<ISequencer>();
        sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(1);
        var createdAt = new DateTime(2025, 5, 3, 8, 0, 0);
        var queue = new AntrianModel(
            "ADM-Q1", DateOnly.FromDateTime(createdAt), TimeOnly.MinValue, TimeOnly.MaxValue,
            "tag", "Loket", new ServicePointType("ADM", "Loket Admisi"), [], sequencer.Object);
        queue.AddEntry(createdAt);
        var repo = new Mock<IAntrianRepo>();
        repo.Setup(x => x.LoadEntity(It.IsAny<IAntrianKey>())).Returns(MayBe.From(queue));
        repo.Setup(x => x.TrySaveWaitingToInServiceTransition(
                It.IsAny<AntrianModel>(), It.IsAny<AntrianEntryModel>()))
            .Returns(false);

        var resolver = new AdmissionServicePointResolver(Options.Create(new AdmisiRajalOptions()));
        var sut = new AdmissionQueueStartHandler(
            repo.Object, TestTglJamProvider.Instance, resolver);

        var act = () => sut.Handle(
            new AdmissionQueueStartCmd("ADM-Q1", 1, "user1"), CancellationToken.None);

        await act.Should().ThrowAsync<AdmissionQueueConcurrencyException>();
        repo.Verify(x => x.SaveChanges(It.IsAny<AntrianModel>()), Times.Never);
    }

    [Fact]
    public async Task Start_WhenQueueIsNotAdmissionServicePoint_ThenRejects()
    {
        var sequencer = new Mock<ISequencer>();
        sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(1);
        var createdAt = new DateTime(2025, 5, 3, 8, 0, 0);
        var queue = new AntrianModel(
            "OTHER-Q1", DateOnly.FromDateTime(createdAt), TimeOnly.MinValue, TimeOnly.MaxValue,
            "tag", "Other", new ServicePointType("OTHER", "Other"), [], sequencer.Object);
        queue.AddEntry(createdAt);
        var repo = new Mock<IAntrianRepo>();
        repo.Setup(x => x.LoadEntity(It.IsAny<IAntrianKey>())).Returns(MayBe.From(queue));
        var resolver = new AdmissionServicePointResolver(Options.Create(new AdmisiRajalOptions()));
        var sut = new AdmissionQueueStartHandler(
            repo.Object, TestTglJamProvider.Instance, resolver);

        var act = () => sut.Handle(
            new AdmissionQueueStartCmd("OTHER-Q1", 1, "user1"), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not the configured Admisi Rajal Service Point*");
        repo.Verify(x => x.SaveChanges(It.IsAny<AntrianModel>()), Times.Never);
    }
}
