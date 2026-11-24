using System.Data.SqlClient;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class PharmacyLoketServicePointDalTest
{
    private readonly PharmacyLoketServicePointDal _dal;
    private readonly PharmacyLoketServicePointRepo _repo;
    private readonly DatabaseOptions _opt;

    public PharmacyLoketServicePointDalTest()
    {
        var opt = ConnStringHelper.GetTestEnv();
        _opt = opt.Value;
        _dal = new PharmacyLoketServicePointDal(opt);
        _repo = new PharmacyLoketServicePointRepo(_dal);
    }

    private static string NewKey(string prefix = "LOKET") => $"{prefix}_{Guid.NewGuid().ToString("N")[..8]}";
    private static string NewSpId(string prefix = "SP") => $"{prefix}_{Guid.NewGuid().ToString("N")[..8]}";

    private void Cleanup(string loketKey)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("DELETE FROM BILRG_AptLoketServicePoint WHERE LoketKey = @loketKey", new { loketKey });
    }

    [Fact]
    public void Dal_Insert_ListByLoket_And_ListByServicePoint_WorksCorrectly()
    {
        var loketKey = NewKey();
        var sp1 = NewSpId();
        var sp2 = NewSpId();
        var now = new DateTime(2026, 10, 26, 9, 0, 0);

        Cleanup(loketKey);

        try
        {
            _dal.Insert(new ApotekLoketServicePointDto(loketKey, sp1, "admin1", now));
            _dal.Insert(new ApotekLoketServicePointDto(loketKey, sp2, "admin1", now));

            var listLoket = _dal.ListByLoket(loketKey).ToList();
            listLoket.Should().HaveCount(2);
            listLoket.Select(x => x.ServicePointId).Should().Contain(new[] { sp1, sp2 });
            listLoket.All(x => x.CrtUser == "admin1").Should().BeTrue();

            var listSp1 = _dal.ListByServicePoint(sp1).ToList();
            listSp1.Should().ContainSingle(x => x.LoketKey == loketKey);
        }
        finally
        {
            Cleanup(loketKey);
        }
    }

    [Fact]
    public void Dal_Delete_And_DeleteByLoket_RemovesExpectedRows()
    {
        var loketKey = NewKey();
        var sp1 = NewSpId();
        var sp2 = NewSpId();

        Cleanup(loketKey);

        try
        {
            _dal.Insert(new ApotekLoketServicePointDto(loketKey, sp1, "SYSTEM", DateTime.Now));
            _dal.Insert(new ApotekLoketServicePointDto(loketKey, sp2, "SYSTEM", DateTime.Now));

            _dal.Delete(loketKey, sp1);
            var remaining = _dal.ListByLoket(loketKey).ToList();
            remaining.Should().ContainSingle(x => x.ServicePointId == sp2);

            _dal.DeleteByLoket(loketKey);
            _dal.ListByLoket(loketKey).Should().BeEmpty();
        }
        finally
        {
            Cleanup(loketKey);
        }
    }

    [Fact]
    public void Dal_Replace_PerformsBatchDeleteAndBulkInsert_Transactionally()
    {
        var loketKey = NewKey();
        var spOld = NewSpId();
        var spNew1 = NewSpId();
        var spNew2 = NewSpId();
        var actor = "SUPERVISOR";
        var at = new DateTime(2026, 10, 26, 10, 30, 0);

        Cleanup(loketKey);

        try
        {
            // Initial mapping
            _dal.Insert(new ApotekLoketServicePointDto(loketKey, spOld, "SYSTEM", DateTime.Now));

            // Replace with new service points (including duplicate & whitespace to test normalization)
            _dal.Replace(loketKey, new[] { $"  {spNew1}  ", spNew2, spNew1, "" }, actor, at);

            var result = _dal.ListByLoket(loketKey).ToList();
            result.Should().HaveCount(2);
            result.Select(x => x.ServicePointId).Should().BeEquivalentTo(new[] { spNew1, spNew2 });
            result.All(x => x.CrtUser == actor).Should().BeTrue();
            result.All(x => x.CrtDate == at).Should().BeTrue();

            // Replace with empty clears all mappings
            _dal.Replace(loketKey, Array.Empty<string>(), actor, at);
            _dal.ListByLoket(loketKey).Should().BeEmpty();
        }
        finally
        {
            Cleanup(loketKey);
        }
    }

    [Fact]
    public void Repo_ListServicePointsByLoket_And_ReplaceServicePoints_IntegratesCorrectly()
    {
        var loketKey = NewKey();
        var sp1 = NewSpId();
        var sp2 = NewSpId();
        var actor = "ADMIN_USER";
        var timestamp = new DateTime(2026, 10, 26, 11, 0, 0);

        Cleanup(loketKey);

        try
        {
            // 1. Initially empty
            _repo.ListServicePointsByLoket(loketKey).Should().BeEmpty();
            _repo.ListMappingsByLoket(loketKey).Should().BeEmpty();

            // 2. Replace with 2 service points
            _repo.ReplaceServicePoints(loketKey, new[] { sp1, sp2 }, actor, timestamp);

            var spList = _repo.ListServicePointsByLoket(loketKey);
            spList.Should().BeEquivalentTo(new[] { sp1, sp2 });

            var modelList = _repo.ListMappingsByLoket(loketKey);
            modelList.Should().HaveCount(2);
            modelList.Should().Contain(x => x.LoketKey == loketKey && x.ServicePointId == sp1 && x.CrtUser == actor);
            modelList.Should().Contain(x => x.LoketKey == loketKey && x.ServicePointId == sp2 && x.CrtUser == actor);

            // 3. Replace with subset (sp2 only)
            _repo.ReplaceServicePoints(loketKey, new[] { sp2 }, actor, timestamp);
            _repo.ListServicePointsByLoket(loketKey).Should().BeEquivalentTo(new[] { sp2 });

            // 4. Input validation
            Action blankLoket = () => _repo.ReplaceServicePoints("", new[] { sp1 }, actor, timestamp);
            blankLoket.Should().Throw<ArgumentException>().WithParameterName("loketKey");
        }
        finally
        {
            Cleanup(loketKey);
        }
    }
}
