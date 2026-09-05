using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Application.ApotekContext.IntegrationFeature.UseCases;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.ApotekContext.IntegrationFeature;

public class AptIntegrationProcessCmdTest
{
    [Fact]
    public void Handle_ProcessesPendingBatchThroughWorker()
    {
        var repo = new Mock<IAptIntegrationTaskRepo>();
        var handler = new RecordingIntegrationHandler();
        var t1 = NewTask("AITP00000001");
        var t2 = NewTask("AITP00000002");
        var map = new Dictionary<string, AptIntegrationTaskModel>
        {
            [t1.IntegrationTaskId] = t1,
            [t2.IntegrationTaskId] = t2
        };
        repo.Setup(x => x.ListPending(It.IsAny<int>())).Returns([t1, t2]);
        repo.Setup(x => x.LoadEntity(It.IsAny<IAptIntegrationTaskKey>()))
            .Returns((IAptIntegrationTaskKey key) =>
                map.TryGetValue(key.IntegrationTaskId, out var t)
                    ? MayBe.From(t)
                    : MayBe<AptIntegrationTaskModel>.None);
        repo.Setup(x => x.ClaimPending(It.IsAny<IAptIntegrationTaskKey>())).Returns(true);

        var sut = new AptIntegrationProcessHandler(new AptIntegrationWorker(repo.Object, [handler]), new AllowAllAuth());
        var result = sut.Handle(new AptIntegrationProcessCmd("op-1", 10), CancellationToken.None)
            .GetAwaiter().GetResult();

        result.ProcessedCount.Should().Be(2);
        result.SucceededCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        handler.Calls.Should().Be(2);
    }

    [Fact]
    public void Handle_NonPositiveBatchSize_FallsBackToDefault()
    {
        var repo = new Mock<IAptIntegrationTaskRepo>();
        repo.Setup(x => x.ListPending(20)).Returns([]);

        var sut = new AptIntegrationProcessHandler(new AptIntegrationWorker(repo.Object, []), new AllowAllAuth());
        sut.Handle(new AptIntegrationProcessCmd("op-1", 0), CancellationToken.None);

        repo.Verify(x => x.ListPending(20), Times.Once);
    }

    private static AptIntegrationTaskModel NewTask(string id)
        => AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerServedAt,
            AptIntegrationSourceKindEnum.Dispensing,
            "ADP" + id[^8..],
            id + ":START",
            AptIntegrationDestinationEnum.Tracker,
            "{}");
}
