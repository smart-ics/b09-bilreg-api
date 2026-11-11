using System.Data.SqlClient;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class PharmacyDisplayLoketDalTest
{
    private readonly PharmacyDisplayLoketDal _dal;
    private readonly PharmacyDisplayLoketRepo _repo;

    public PharmacyDisplayLoketDalTest()
    {
        var opt = ConnStringHelper.GetTestEnv();
        _dal = new PharmacyDisplayLoketDal(opt);
        _repo = new PharmacyDisplayLoketRepo(_dal);
    }

    private static string NewId(string prefix = "DISP") => $"{prefix}{Guid.NewGuid().ToString("N")[..8]}";

    [Fact]
    public void Dal_Insert_GetData_And_ListByDisplayId_OrdersCorrectly()
    {
        var displayId = NewId();
        var loket1 = "LOKET-01";
        var loket2 = "LOKET-02";

        Cleanup(displayId);

        try
        {
            _dal.Insert(new PharmacyDisplayLoketDto(displayId, loket1, 2));
            _dal.Insert(new PharmacyDisplayLoketDto(displayId, loket2, 1));

            var data1 = _dal.GetData(displayId, loket1);
            data1.Should().NotBeNull();
            data1!.DisplayId.Should().Be(displayId);
            data1.LoketKey.Should().Be(loket1);
            data1.SortOrder.Should().Be(2);

            var list = _dal.ListByDisplayId(displayId).ToList();
            list.Should().HaveCount(2);
            list[0].LoketKey.Should().Be(loket2); // SortOrder 1 comes first
            list[1].LoketKey.Should().Be(loket1); // SortOrder 2 comes second

            _dal.Delete(displayId, loket1);
            _dal.GetData(displayId, loket1).Should().BeNull();
        }
        finally
        {
            Cleanup(displayId);
        }
    }

    [Fact]
    public void Repo_SaveMapping_IsIdempotent_And_ListByDisplayId_ReturnsModels()
    {
        var displayId = NewId();
        var loket1 = "LOKET-01";

        Cleanup(displayId);

        try
        {
            // Initial save
            _repo.SaveMapping(displayId, loket1, 1);

            var list1 = _repo.ListByDisplayId(displayId);
            list1.Should().ContainSingle(x => x.DisplayId == displayId && x.LoketKey == loket1 && x.SortOrder == 1);

            // Re-save with updated SortOrder
            _repo.SaveMapping(displayId, loket1, 5);

            var list2 = _repo.ListByDisplayId(displayId);
            list2.Should().ContainSingle(x => x.DisplayId == displayId && x.LoketKey == loket1 && x.SortOrder == 5);

            // Delete
            _repo.DeleteMapping(displayId, loket1);
            _repo.ListByDisplayId(displayId).Should().BeEmpty();
        }
        finally
        {
            Cleanup(displayId);
        }
    }

    private static void Cleanup(string displayId)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("DELETE FROM BILRG_AptDisplayLoket WHERE DisplayId = @displayId", new { displayId });
    }
}
