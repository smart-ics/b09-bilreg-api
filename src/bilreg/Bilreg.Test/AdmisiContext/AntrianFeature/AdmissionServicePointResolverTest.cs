using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionServicePointResolverTest
{
    [Theory]
    [InlineData("BPJS")]
    [InlineData("UMUM")]
    [InlineData("PRIORITY")]
    public void EnsureAdmissionQueue_WhenServicePointIsRegistered_AcceptsDynamicPoint(string id)
    {
        var master = AdmissionServicePointModel.Create(id, id, id[..1]);
        var resolver = ResolverReturning(master);
        var queue = Queue(id);

        var act = () => resolver.EnsureAdmissionQueue(queue);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureAdmissionQueue_WhenServicePointWasRetired_AllowsExistingQueueToFinish()
    {
        var retired = AdmissionServicePointModel.Create("BPJS", "BPJS", "B").Retire();
        var resolver = ResolverReturning(retired);

        var act = () => resolver.EnsureAdmissionQueue(Queue("BPJS"));

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureAdmissionQueue_WhenServicePointIsUnknown_RejectsUnrelatedQueue()
    {
        var repo = new Mock<IAdmissionServicePointRepo>();
        var resolver = new AdmissionServicePointResolver(repo.Object);

        var act = () => resolver.EnsureAdmissionQueue(Queue("DOCTOR"));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*does not belong to a registered Admission Service Point*");
    }

    private static AdmissionServicePointResolver ResolverReturning(AdmissionServicePointModel point)
    {
        var repo = new Mock<IAdmissionServicePointRepo>();
        repo.Setup(x => x.LoadEntity(It.IsAny<IAdmissionServicePointKey>()))
            .Returns(MayBe.From(point));
        return new AdmissionServicePointResolver(repo.Object);
    }

    private static AntrianModel Queue(string servicePointId)
    {
        var sequencer = new Mock<ISequencer>();
        return new AntrianModel(
            $"Q-{servicePointId}",
            new DateOnly(2026, 7, 27),
            TimeOnly.MinValue,
            TimeOnly.MaxValue,
            $"tag-{servicePointId}",
            servicePointId,
            new ServicePointType(servicePointId, servicePointId),
            [],
            sequencer.Object);
    }
}
