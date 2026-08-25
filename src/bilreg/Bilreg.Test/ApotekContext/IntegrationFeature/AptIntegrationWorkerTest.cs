using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ApotekContext.IntegrationFeature;

public class AptIntegrationWorkerTest
{
    private readonly Mock<IAptIntegrationTaskRepo> _repo = new();
    private readonly RecordingHandler _handler = new();

    private AptIntegrationWorker CreateSut()
        => new(_repo.Object, [_handler]);

    [Fact]
    public void ProcessOne_Success_MarksSucceededAndDoesNotDuplicateHandlerOnReplay()
    {
        var task = AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerServedAt,
            AptIntegrationSourceKindEnum.Dispensing,
            "ADP000000001",
            "ADP000000001:START",
            AptIntegrationDestinationEnum.Tracker,
            "{}");

        SetupLoad(task);
        _repo.Setup(x => x.ClaimPending(It.IsAny<IAptIntegrationTaskKey>())).Returns(true);

        var sut = CreateSut();
        var first = sut.ProcessOne(task.IntegrationTaskId);
        first.Success.Should().BeTrue();
        _handler.Calls.Should().Be(1);
        task.TaskStatus.Should().Be(AptIntegrationTaskStatusEnum.Succeeded);

        var second = sut.ProcessOne(task.IntegrationTaskId);
        second.Success.Should().BeTrue();
        second.Message.Should().Be("idempotent");
        _handler.Calls.Should().Be(1);
    }

    [Fact]
    public void ProcessOne_ClaimLost_DoesNotCallHandler()
    {
        var task = AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerServedAt,
            AptIntegrationSourceKindEnum.Dispensing,
            "ADP000000002",
            "ADP000000002:START",
            AptIntegrationDestinationEnum.Tracker,
            "{}");
        SetupLoad(task);
        _repo.Setup(x => x.ClaimPending(It.IsAny<IAptIntegrationTaskKey>())).Returns(false);

        var result = CreateSut().ProcessOne(task.IntegrationTaskId);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Claim lost");
        _handler.Calls.Should().Be(0);
    }

    [Fact]
    public void ProcessOne_HandlerFailure_MarksFailed()
    {
        var task = AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerServedAt,
            AptIntegrationSourceKindEnum.Dispensing,
            "ADP000000003",
            "ADP000000003:START",
            AptIntegrationDestinationEnum.Tracker,
            "{}");
        SetupLoad(task);
        _repo.Setup(x => x.ClaimPending(It.IsAny<IAptIntegrationTaskKey>())).Returns(true);
        _handler.Fail = true;

        var result = CreateSut().ProcessOne(task.IntegrationTaskId);

        result.Success.Should().BeFalse();
        task.TaskStatus.Should().Be(AptIntegrationTaskStatusEnum.Failed);
        task.RetryCount.Should().Be(1);
        task.LastError.Should().Be("boom");
    }

    [Fact]
    public void ProcessOne_FailedTask_IsRejectedWithoutHandlerCall()
    {
        var task = AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerServedAt,
            AptIntegrationSourceKindEnum.Dispensing,
            "ADP000000004",
            "ADP000000004:START",
            AptIntegrationDestinationEnum.Tracker,
            "{}");
        task.ClaimPending();
        task.MarkFailed("boom");
        SetupLoad(task);
        _repo.Setup(x => x.ClaimPending(It.IsAny<IAptIntegrationTaskKey>())).Returns(true);

        var result = CreateSut().ProcessOne(task.IntegrationTaskId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Failed");
        _handler.Calls.Should().Be(0);
    }

    [Fact]
    public void ProcessBatch_ProcessesListedPendingItems()
    {
        var t1 = AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerServedAt,
            AptIntegrationSourceKindEnum.Dispensing,
            "ADP1", "ADP1:START", AptIntegrationDestinationEnum.Tracker, "{}");
        var t2 = AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerServedAt,
            AptIntegrationSourceKindEnum.Dispensing,
            "ADP2", "ADP2:START", AptIntegrationDestinationEnum.Tracker, "{}");

        var map = new Dictionary<string, AptIntegrationTaskModel>
        {
            [t1.IntegrationTaskId] = t1,
            [t2.IntegrationTaskId] = t2
        };
        _repo.Setup(x => x.ListPending(It.IsAny<int>())).Returns([t1, t2]);
        _repo.Setup(x => x.LoadEntity(It.IsAny<IAptIntegrationTaskKey>()))
            .Returns((IAptIntegrationTaskKey key) =>
                map.TryGetValue(key.IntegrationTaskId, out var t)
                    ? MayBe.From(t)
                    : MayBe<AptIntegrationTaskModel>.None);
        _repo.Setup(x => x.ClaimPending(It.IsAny<IAptIntegrationTaskKey>())).Returns(true);

        var result = CreateSut().ProcessBatch(20);
        result.ProcessedCount.Should().Be(2);
        result.SucceededCount.Should().Be(2);
        _handler.Calls.Should().Be(2);
    }

    private void SetupLoad(AptIntegrationTaskModel task)
    {
        _repo.Setup(x => x.LoadEntity(It.IsAny<IAptIntegrationTaskKey>()))
            .Returns((IAptIntegrationTaskKey key) =>
                key.IntegrationTaskId == task.IntegrationTaskId
                    ? MayBe.From(task)
                    : MayBe<AptIntegrationTaskModel>.None);
    }

    private sealed class RecordingHandler : IAptIntegrationHandler
    {
        public AptIntegrationTaskTypeEnum TaskType => AptIntegrationTaskTypeEnum.TrackerServedAt;
        public int Calls { get; private set; }
        public bool Fail { get; set; }

        public AptIntegrationHandleResult Handle(AptIntegrationTaskModel task)
        {
            Calls++;
            return Fail
                ? new AptIntegrationHandleResult(false, "", "boom")
                : new AptIntegrationHandleResult(true, "corr-1", null);
        }
    }
}
