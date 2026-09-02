using System.Data.SqlClient;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.ApotekContext.IntegrationFeature;
using Bilreg.Infrastructure.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.ApotekContext.IntegrationFeature;

public class AptIntegrationTaskDalTest
{
    private readonly AptIntegrationTaskDal _taskDal;
    private readonly QueueCloseDal _closeDal;

    public AptIntegrationTaskDalTest()
    {
        var opt = ConnStringHelper.GetTestEnv();
        _taskDal = new(opt);
        _closeDal = new(opt);
    }

    [Fact]
    public void Business_save_and_task_insert_commit_together()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var closeId = $"QC{run}";
        var antrianId = $"ANTX-{run}";
        var taskId = $"AT{run}C";
        var idempotencyKey = $"{antrianId}:WDN:{run}";
        Cleanup(closeId, taskId);

        using (var trans = TransHelper.NewScope())
        {
            _closeDal.Insert(NewClose(closeId, antrianId));
            _taskDal.Insert(NewTask(taskId, idempotencyKey));
            trans.Complete();
        }

        ReadClose(closeId).Should().NotBeNull();
        ReadClose(closeId)!.QueueCloseId.Should().Be(closeId);
        var persisted = ReadTask(taskId);
        persisted.Should().NotBeNull();
        persisted!.IdempotencyKey.Should().Be(idempotencyKey);
        persisted.TaskStatus.Should().Be(AptIntegrationTaskStatusEnum.Pending);

        Cleanup(closeId, taskId);
    }

    [Fact]
    public void Business_save_and_task_insert_roll_back_together()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var closeId = $"QC{run}";
        var antrianId = $"ANTX-{run}";
        var taskId = $"AT{run}R";

        using (var trans = TransHelper.NewScope())
        {
            _closeDal.Insert(NewClose(closeId, antrianId));
            _taskDal.Insert(NewTask(taskId, $"{antrianId}:WDN:{run}"));
            // no Complete: disposing the scope must roll both writes back
        }

        ReadClose(closeId).Should().BeNull();
        ReadTask(taskId).Should().BeNull();
    }

    [Fact]
    public void ListPending_and_ClaimPending_target_only_pending_status()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var pendingId = $"AT{run}P";
        var failedId = $"AT{run}F";
        var deadId = $"AT{run}D";
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);

        using var scope = TransHelper.NewScope();
        _taskDal.Insert(NewTask(pendingId, $"K:{run}:P", baseTime.AddMinutes(1)));
        var failed = NewTask(failedId, $"K:{run}:F", baseTime.AddMinutes(2));
        failed.ClaimPending();
        failed.MarkFailed("boom");
        _taskDal.Update(failed);
        var dead = NewTask(deadId, $"K:{run}:D", baseTime.AddMinutes(3), AptIntegrationTaskStatusEnum.Dead);
        _taskDal.Insert(dead);

        var listed = _taskDal.ListPending(10).Select(x => x.IntegrationTaskId).ToList();
        listed.Should().Contain(pendingId);
        listed.Should().NotContain(failedId);
        listed.Should().NotContain(deadId);

        _taskDal.ClaimPending(AptIntegrationTaskModel.Key(failedId)).Should().BeFalse();
        _taskDal.ClaimPending(AptIntegrationTaskModel.Key(deadId)).Should().BeFalse();
        _taskDal.ClaimPending(AptIntegrationTaskModel.Key(pendingId)).Should().BeTrue();
        // no Complete: the whole probe rolls back and leaves the database clean
    }

    private static QueueCloseDto NewClose(string closeId, string antrianId)
    {
        var now = DateTime.Now;
        return new QueueCloseDto(closeId, antrianId, 1, "dal-contract", "tester", now,
            "tester", now, "", new DateTime(3000, 1, 1), "", new DateTime(3000, 1, 1));
    }

    private static AptIntegrationTaskModel NewTask(
        string taskId, string idempotencyKey, DateTime? createdAt = null,
        AptIntegrationTaskStatusEnum status = AptIntegrationTaskStatusEnum.Pending)
        => AptIntegrationTaskModel.Rehydrate(
            taskId,
            AptIntegrationTaskTypeEnum.TrackerWithdrawn,
            AptIntegrationSourceKindEnum.QueueClose,
            "SRC" + taskId[2..],
            idempotencyKey,
            AptIntegrationDestinationEnum.Tracker,
            "{}",
            status,
            0,
            "",
            new DateTime(3000, 1, 1),
            new DateTime(3000, 1, 1),
            "",
            createdAt ?? DateTime.Now);

    private QueueCloseDto? ReadClose(string closeId)
        => _closeDal.GetData(QueueCloseModel.Key(closeId));

    private AptIntegrationTaskModel? ReadTask(string taskId)
        => _taskDal.GetData(AptIntegrationTaskModel.Key(taskId));

    private void Cleanup(string closeId, string taskId)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("DELETE FROM BILRG_AptQueueClose WHERE QueueCloseId=@id", new { id = closeId });
        conn.Execute("DELETE FROM BILRG_AptIntegrationTask WHERE IntegrationTaskId=@id", new { id = taskId });
    }
}
