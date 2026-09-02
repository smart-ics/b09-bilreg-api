using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Bilreg.Application.ApotekContext.WorklistFeature;
using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Infrastructure.ApotekContext.DispensingFeature;
using Bilreg.Infrastructure.ApotekContext.WorklistFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.WorklistFeature;

public class AptWorklistDispensingSerahDalTest
{
    private static readonly object SchemaLock = new();

    private readonly DispensingRepo _repo;
    private readonly AptWorklistDal _worklist;

    public AptWorklistDispensingSerahDalTest()
    {
        EnsureSchema();
        var opt = ConnStringHelper.GetTestEnv();
        _repo = new(new DispensingDal(opt), new DispensingItemDal(opt), new FinalReviewDal(opt));
        _worklist = new(opt);
    }

    [Fact]
    public void ListDispensing_returns_only_released_and_preparing_rows()
    {
        var run = Guid.NewGuid().ToString("N")[..6];
        var released = EstablishReleased($"SO{run}R");
        var preparing = EstablishPreparing($"SO{run}P");
        var prepared = EstablishPrepared($"SO{run}D");
        Cleanup(released.DispensingId, preparing.DispensingId, prepared.DispensingId);

        _repo.SaveChanges(released);
        _repo.SaveChanges(preparing);
        _repo.SaveChanges(prepared);

        var items = _worklist.ListDispensing();

        items.Should().Contain(x => x.DispensingId == released.DispensingId && x.Status == DispensingStatusEnum.Released);
        items.Should().Contain(x => x.DispensingId == preparing.DispensingId && x.Status == DispensingStatusEnum.Preparing);
        items.Should().NotContain(x => x.DispensingId == prepared.DispensingId);

        Cleanup(released.DispensingId, preparing.DispensingId, prepared.DispensingId);
    }

    [Fact]
    public void ListSerah_projects_pickup_expired_from_prepared_at_and_asOf_clock()
    {
        var run = Guid.NewGuid().ToString("N")[..6];
        var preparedAt = new DateTime(2026, 8, 10, 9, 0, 0);
        var model = EstablishPrepared($"SO{run}S", preparedAt);
        Cleanup(model.DispensingId);
        _repo.SaveChanges(model);

        var insideWindow = _worklist.ListSerah(preparedAt.AddDays(5), 7);
        insideWindow.Should().ContainSingle(x =>
            x.DispensingId == model.DispensingId
            && x.Category == SerahWorklistProjection.ReadyForPickup);

        var outsideWindow = _worklist.ListSerah(preparedAt.AddDays(8), 7);
        outsideWindow.Should().ContainSingle(x =>
            x.DispensingId == model.DispensingId
            && x.Category == SerahWorklistProjection.PickupExpired);

        Cleanup(model.DispensingId);
    }

    [Fact]
    public void ListSerah_maps_expired_no_show_to_AccountablyResolved()
    {
        var run = Guid.NewGuid().ToString("N")[..6];
        var preparedAt = new DateTime(2026, 8, 10, 9, 0, 0);
        var model = EstablishPrepared($"SO{run}E", preparedAt);
        model.ExpireNoShow(preparedAt.AddDays(10), "no show");
        Cleanup(model.DispensingId);
        _repo.SaveChanges(model);

        var items = _worklist.ListSerah(preparedAt.AddDays(20), 7);
        items.Should().ContainSingle(x =>
            x.DispensingId == model.DispensingId
            && x.Category == SerahWorklistProjection.AccountablyResolved);

        Cleanup(model.DispensingId);
    }

    private static DispensingModel EstablishReleased(string salesOrderId)
    {
        var at = DateTime.Now;
        var model = DispensingModel.Establish(
            salesOrderId,
            ApotekLocationIds.PharmacyUnitLayananId,
            ApotekLocationIds.DispensingTemporaryUnitLayananId,
            [new DispensingItemModel(1, 1, "BRG1", 1m, "", "", "", DispensingItemOutcomeEnum.Open)]);
        model.Release(at, true);
        return model;
    }

    private static DispensingModel EstablishPreparing(string salesOrderId)
    {
        var at = DateTime.Now;
        var model = DispensingModel.Establish(
            salesOrderId,
            ApotekLocationIds.PharmacyUnitLayananId,
            ApotekLocationIds.DispensingTemporaryUnitLayananId,
            [new DispensingItemModel(1, 1, "BRG1", 1m, "", "", "", DispensingItemOutcomeEnum.Open)]);
        model.Release(at.AddMinutes(-5), true);
        model.StartPreparation(at, true);
        return model;
    }

    private static DispensingModel EstablishPrepared(string salesOrderId, DateTime? preparedAt = null)
    {
        var at = preparedAt ?? DateTime.Now;
        var model = DispensingModel.Establish(
            salesOrderId,
            ApotekLocationIds.PharmacyUnitLayananId,
            ApotekLocationIds.DispensingTemporaryUnitLayananId,
            [new DispensingItemModel(1, 1, "BRG1", 1m, "", "", "", DispensingItemOutcomeEnum.Open)]);
        model.Release(at.AddMinutes(-10), true);
        model.StartPreparation(at.AddMinutes(-5), true);
        model.MarkPrepared(at);
        return model;
    }

    private static void Cleanup(params string[] dispensingIds)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        foreach (var id in dispensingIds)
        {
            conn.Execute("DELETE FROM BILRG_AptFinalReview WHERE DispensingId=@id", new { id });
            conn.Execute("DELETE FROM BILRG_AptDispensingItem WHERE DispensingId=@id", new { id });
            conn.Execute("DELETE FROM BILRG_AptDispensing WHERE DispensingId=@id", new { id });
        }
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
        var exists = conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME=@name", new { name = tableName });
        if (exists > 0)
            return;
        var script = File.ReadAllText(Path.Combine(sqlRoot, scriptFile));
        script = Regex.Replace(script, @"CREATE\s+TABLE", "CREATE TABLE", RegexOptions.IgnoreCase);
        conn.Execute(script);
    }

    private static string FindSqlRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            var candidate = Path.Combine(dir, "bilreg", "Bilreg.SqlDb", "ApotekContext");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new InvalidOperationException("ApotekContext SQL root not found.");
    }
}
