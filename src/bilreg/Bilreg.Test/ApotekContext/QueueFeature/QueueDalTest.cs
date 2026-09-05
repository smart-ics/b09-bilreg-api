using System.Data.SqlClient;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class QueueDalTest
{
    private readonly QueueCloseDal _closeDal;
    private readonly QueueCloseRepo _closeRepo;

    public QueueDalTest()
    {
        var opt = ConnStringHelper.GetTestEnv();
        _closeDal = new(opt);
        _closeRepo = new(_closeDal);
    }

    [Fact]
    public void Close_round_trip_persists_one_fact_per_queue()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var closeId = $"QC{run}";
        var antrianId = $"ANT-CLOSE-{run}";
        var model = QueueCloseModel.Rehydrate(closeId, antrianId, 1, "dal close", "staff", DateTime.Now);
        CleanupClose(closeId);

        _closeRepo.SaveChanges(model);

        var byQueue = _closeRepo.LoadByQueue(antrianId, 1).Value;
        byQueue.QueueCloseId.Should().Be(closeId);
        byQueue.Reason.Should().Be("dal close");
        _closeRepo.LoadEntity(QueueCloseModel.Key(closeId)).Value.StaffId.Should().Be("staff");

        CleanupClose(closeId);
    }

    private void CleanupClose(string closeId)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("DELETE FROM BILRG_AptQueueClose WHERE QueueCloseId=@closeId", new { closeId });
    }
}
