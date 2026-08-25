using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Application.ApotekContext.IntegrationFeature.UseCases;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.ApotekContext.IntegrationFeature;

public class AptIntegrationRetryCommandTest
{
    [Fact]
    public void Retry_OnFailedTask_PersistsPending_AndProcesses()
    {
        var (sut, repo, handler, persisted) = CreateSut();
        var task = NewTask("AITR0000000F");
        task.ClaimPending();
        task.MarkFailed("boom");
        SetupLoad(repo, task);
        repo.Setup(x => x.ClaimPending(It.IsAny<IAptIntegrationTaskKey>())).Returns(true);

        var result = sut.Handle(new AptIntegrationRetryCmd("op-1", task.IntegrationTaskId), CancellationToken.None)
            .GetAwaiter().GetResult();

        result.Success.Should().BeTrue();
        persisted.Should().Contain(AptIntegrationTaskStatusEnum.Pending);
        handler.Calls.Should().Be(1);
        task.TaskStatus.Should().Be(AptIntegrationTaskStatusEnum.Succeeded);
    }

    [Fact]
    public void Retry_OnStaleProcessingRow_ReclaimsAndProcesses()
    {
        var (sut, repo, handler, persisted) = CreateSut();
        var staleAt = DateTime.Now.AddMinutes(-AptIntegrationTaskModel.StaleProcessingMinutes - 1);
        var task = Rehydrated("AITR0000000S", AptIntegrationTaskStatusEnum.Processing, staleAt);
        SetupLoad(repo, task);
        repo.Setup(x => x.ClaimPending(It.IsAny<IAptIntegrationTaskKey>())).Returns(true);

        var result = sut.Handle(new AptIntegrationRetryCmd("op-1", task.IntegrationTaskId), CancellationToken.None)
            .GetAwaiter().GetResult();

        result.Success.Should().BeTrue();
        persisted.Should().Contain(AptIntegrationTaskStatusEnum.Pending);
        handler.Calls.Should().Be(1);
        task.TaskStatus.Should().Be(AptIntegrationTaskStatusEnum.Succeeded);
    }

    [Fact]
    public void Retry_OnFreshProcessingRow_ThrowsWithoutPersist()
    {
        var (sut, repo, handler, persisted) = CreateSut();
        var task = Rehydrated("AITR0000000Q", AptIntegrationTaskStatusEnum.Processing, DateTime.Now);
        SetupLoad(repo, task);
        repo.Setup(x => x.SaveChanges(It.IsAny<AptIntegrationTaskModel>()));

        var act = () => sut.Handle(new AptIntegrationRetryCmd("op-1", task.IntegrationTaskId), CancellationToken.None);

        act.Should().ThrowAsync<InvalidOperationException>();
        persisted.Should().BeEmpty();
        handler.Calls.Should().Be(0);
    }

    private static (AptIntegrationRetryHandler Sut, Mock<IAptIntegrationTaskRepo> Repo,
        RecordingIntegrationHandler Handler, List<AptIntegrationTaskStatusEnum> Persisted) CreateSut()
    {
        var repo = new Mock<IAptIntegrationTaskRepo>();
        var handler = new RecordingIntegrationHandler();
        var persisted = new List<AptIntegrationTaskStatusEnum>();
        repo.Setup(x => x.SaveChanges(It.IsAny<AptIntegrationTaskModel>()))
            .Callback<AptIntegrationTaskModel>(t => persisted.Add(t.TaskStatus));
        repo.Setup(x => x.ClaimPending(It.IsAny<IAptIntegrationTaskKey>())).Returns(true);
        var sut = new AptIntegrationRetryHandler(repo.Object, new AptIntegrationWorker(repo.Object, [handler]), new AllowAllAuth());
        return (sut, repo, handler, persisted);
    }

    private static void SetupLoad(Mock<IAptIntegrationTaskRepo> repo, AptIntegrationTaskModel task)
        => repo.Setup(x => x.LoadEntity(It.IsAny<IAptIntegrationTaskKey>()))
            .Returns((IAptIntegrationTaskKey key) =>
                key.IntegrationTaskId == task.IntegrationTaskId
                    ? MayBe.From(task)
                    : MayBe<AptIntegrationTaskModel>.None);

    private static AptIntegrationTaskModel NewTask(string id)
        => AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerServedAt,
            AptIntegrationSourceKindEnum.Dispensing,
            "ADP" + id[^8..],
            id + ":START",
            AptIntegrationDestinationEnum.Tracker,
            "{}");

    private static AptIntegrationTaskModel Rehydrated(
        string id, AptIntegrationTaskStatusEnum status, DateTime processedDate)
        => AptIntegrationTaskModel.Rehydrate(
            id,
            AptIntegrationTaskTypeEnum.TrackerServedAt,
            AptIntegrationSourceKindEnum.Dispensing,
            "ADP" + id[^8..],
            id + ":START",
            AptIntegrationDestinationEnum.Tracker,
            "{}",
            status,
            0,
            "",
            new DateTime(3000, 1, 1),
            processedDate,
            "",
            DateTime.Now);
}
