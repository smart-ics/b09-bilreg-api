using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Infrastructure.ApotekContext.DispensingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.DispensingFeature;

public class DispensingDalTest
{
    private static readonly object SchemaLock = new();

    private readonly DispensingDal _dal;
    private readonly DispensingItemDal _itemDal;
    private readonly FinalReviewDal _reviewDal;
    private readonly DispensingRepo _repo;

    public DispensingDalTest()
    {
        EnsureSchema();
        var opt = ConnStringHelper.GetTestEnv();
        _dal = new(opt);
        _itemDal = new(opt);
        _reviewDal = new(opt);
        _repo = new(_dal, _itemDal, _reviewDal);
    }

    [Fact]
    public void RoundTrip_preserves_header_items_and_preparation_timestamps()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var salesOrderId = $"ASO{run}";
        var startedAt = new DateTime(2026, 8, 27, 10, 0, 0);
        var preparedAt = startedAt.AddMinutes(20);
        var model = DispensingModel.Establish(
            salesOrderId,
            ApotekLocationIds.PharmacyUnitLayananId,
            ApotekLocationIds.DispensingTemporaryUnitLayananId,
            [new DispensingItemModel(1, 1, $"BRG{run}", 4m, "", "", "", DispensingItemOutcomeEnum.Open)]);
        model.Release(startedAt.AddMinutes(-5), true);
        model.StartPreparation(startedAt, true);
        model.MarkPrepared(preparedAt);
        Cleanup(model.DispensingId);

        _repo.SaveChanges(model);

        var loaded = _repo.LoadEntity(DispensingModel.Key(model.DispensingId)).Value;
        loaded.SalesOrderId.Should().Be(salesOrderId);
        loaded.PharmacyUnitLayananId.Should().Be(ApotekLocationIds.PharmacyUnitLayananId);
        loaded.TemporaryUnitLayananId.Should().Be(ApotekLocationIds.DispensingTemporaryUnitLayananId);
        loaded.DispensingStatus.Should().Be(DispensingStatusEnum.Prepared);
        loaded.PreparationStartedAt.Should().Be(startedAt);
        loaded.PreparedAt.Should().Be(preparedAt);
        loaded.Items.Should().ContainSingle();
        loaded.Items[0].SalesOrderItemNo.Should().Be(1);
        loaded.Items[0].BrgId.Should().Be($"BRG{run}");
        loaded.Items[0].Qty.Should().Be(4m);

        Cleanup(model.DispensingId);
    }

    [Fact]
    public void Update_persists_status_transition_and_version()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var model = DispensingModel.Establish(
            $"ASO{run}",
            ApotekLocationIds.PharmacyUnitLayananId,
            ApotekLocationIds.DispensingTemporaryUnitLayananId,
            [new DispensingItemModel(1, 1, "BRG1", 2m, "", "", "", DispensingItemOutcomeEnum.Open)]);
        Cleanup(model.DispensingId);
        _repo.SaveChanges(model);

        model.AwaitClearance();
        _repo.SaveChanges(model);

        var loaded = _repo.LoadEntity(model).Value;
        loaded.DispensingStatus.Should().Be(DispensingStatusEnum.AwaitingClearance);
        loaded.Version.Should().Be(2);

        Cleanup(model.DispensingId);
    }

    [Fact]
    public void RoundTrip_persists_append_only_final_reviews()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var at = new DateTime(2026, 8, 27, 11, 0, 0);
        var model = DispensingModel.Establish(
            $"ASO{run}",
            ApotekLocationIds.PharmacyUnitLayananId,
            ApotekLocationIds.DispensingTemporaryUnitLayananId,
            [new DispensingItemModel(1, 1, $"BRG{run}", 2m, "", "", "", DispensingItemOutcomeEnum.Open)]);
        model.Release(at.AddMinutes(-10), true);
        model.StartPreparation(at.AddMinutes(-5), true);
        model.MarkPrepared(at);
        model.AppendFinalReview(FinalReviewOutcomeEnum.Fail, "wrong label", "pharm1", at.AddMinutes(1));
        Cleanup(model.DispensingId);
        _repo.SaveChanges(model);

        model.MarkPrepared(at.AddMinutes(20));
        model.AppendFinalReview(FinalReviewOutcomeEnum.Pass, "", "pharm2", at.AddMinutes(21));
        _repo.SaveChanges(model);

        var loaded = _repo.LoadEntity(DispensingModel.Key(model.DispensingId)).Value;
        loaded.Reviews.Should().HaveCount(2);
        loaded.Reviews[0].Outcome.Should().Be(FinalReviewOutcomeEnum.Fail);
        loaded.Reviews[0].PharmacistId.Should().Be("pharm1");
        loaded.Reviews[1].Outcome.Should().Be(FinalReviewOutcomeEnum.Pass);
        loaded.Reviews[1].PharmacistId.Should().Be("pharm2");

        Cleanup(model.DispensingId);
    }

    private static void EnsureSchema()
    {
        lock (SchemaLock)
        {
            var opt = ConnStringHelper.GetTestEnv();
            using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
            conn.Open();

            var sqlRoot = FindSqlRoot();
            EnsureTable(conn, sqlRoot, "BILRG_AptDispensing", "BILRG_AptDispensing.sql");
            EnsureTable(conn, sqlRoot, "BILRG_AptDispensingItem", "BILRG_AptDispensingItem.sql");
            EnsureTable(conn, sqlRoot, "BILRG_AptFinalReview", "BILRG_AptFinalReview.sql");
        }
    }

    private static void EnsureTable(SqlConnection conn, string sqlRoot, string tableName, string scriptFile)
    {
        if (conn.ExecuteScalar<int>("SELECT COUNT(1) FROM sys.tables WHERE name = @tableName", new { tableName }) != 0)
            return;

        ExecuteScript(conn, Path.Combine(sqlRoot, scriptFile));
    }

    private static void ExecuteScript(SqlConnection conn, string scriptPath)
    {
        var script = File.ReadAllText(scriptPath);
        var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        foreach (var batch in batches.Where(x => !string.IsNullOrWhiteSpace(x)))
            conn.Execute(batch);
    }

    private static string FindSqlRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "Bilreg.SqlDb", "ApotekContext");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Unable to locate Bilreg.SqlDb/ApotekContext.");
    }

    private void Cleanup(params string[] ids)
    {
        var opt = ConnStringHelper.GetTestEnv();
        using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
        foreach (var id in ids)
        {
            conn.Execute("DELETE FROM BILRG_AptFinalReview WHERE DispensingId=@id", new { id });
            conn.Execute("DELETE FROM BILRG_AptDispensingItem WHERE DispensingId=@id", new { id });
            conn.Execute("DELETE FROM BILRG_AptDispensing WHERE DispensingId=@id", new { id });
        }
    }
}
