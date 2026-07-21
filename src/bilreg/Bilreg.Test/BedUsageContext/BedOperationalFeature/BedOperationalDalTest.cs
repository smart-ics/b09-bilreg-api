using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Infrastructure.BedUsageContext.BedOperationalFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.BedUsageContext.BedOperationalFeature;

public class BedOperationalDalTest
{
    private static readonly DateTime At =
        new(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc);

    private readonly BedOperationalDal _header =
        new(ConnStringHelper.GetTestEnv());
    private readonly BedReadinessTransactionDal _transactions =
        new(ConnStringHelper.GetTestEnv());
    private readonly BedReadinessCorrectionDal _corrections =
        new(ConnStringHelper.GetTestEnv());

    [Fact]
    public void Header_InsertGetAndConditionalUpdate_RoundTrips()
    {
        EnsureSchema();
        const string bedId = "BDOPD001";
        Cleanup(bedId);
        try
        {
            var source = Create(bedId).VerifyReady(
                "Ready verified",
                "EVIDENCE-1",
                "PEG-1",
                "PEG-2",
                At,
                At.AddMinutes(1),
                string.Empty,
                "REQ-DAL-H1");
            var inserted = BedOperationalDto.FromModel(source);
            _header.Insert(inserted);

            var actual = _header.GetData(source);
            var update = inserted with
            {
                Version = 2,
                OccupancyEpoch = 1
            };
            var affected = _header.UpdateConditional(update, inserted.Version);
            var staleAffected = _header.UpdateConditional(
                update with { Version = 3 },
                inserted.Version);

            actual.Should().BeEquivalentTo(inserted);
            affected.Should().Be(1);
            staleAffected.Should().Be(0);
            _header.GetData(source).OccupancyEpoch.Should().Be(1);
        }
        finally
        {
            Cleanup(bedId);
        }
    }

    [Fact]
    public void Detail_BulkInsertAndList_PreservesChronologyAndCorrectionLinks()
    {
        EnsureSchema();
        const string bedId = "BDOPD002";
        Cleanup(bedId);
        try
        {
            var ready = Create(bedId).VerifyReady(
                "Ready verified",
                "EVIDENCE-1",
                "PEG-1",
                "PEG-2",
                At,
                At.AddMinutes(1),
                string.Empty,
                "REQ-DAL-D1");
            var originalId = ready.ListReadinessTransaction.Single().TransactionId;
            var source = ready.CorrectReadiness(
                originalId,
                BedReadinessStatusEnum.OutOfService,
                BedRestrictionTypeEnum.Maintenance,
                "Maintenance required",
                "Ready fact was incorrect",
                "EVIDENCE-2",
                "PEG-HEAD",
                string.Empty,
                At.AddHours(1),
                At.AddHours(1).AddMinutes(1),
                "REQ-DAL-D2");
            _transactions.Insert(source.ListReadinessTransaction
                .Select(BedReadinessTransactionDto.FromModel));
            _corrections.Insert(source.ListReadinessCorrection
                .Select(BedReadinessCorrectionDto.FromModel));

            var transactions = _transactions.ListData(source).ToList();
            var corrections = _corrections.ListData(source).ToList();

            transactions.Should().HaveCount(2);
            transactions.Should().BeInAscendingOrder(x => x.OccurredAt);
            corrections.Should().ContainSingle();
            corrections.Single().OriginalTransactionId.Should().Be(originalId);
            corrections.Single().ReplacementTransactionId.Should()
                .Be(source.ListReadinessCorrection.Single().ReplacementTransactionId);
        }
        finally
        {
            Cleanup(bedId);
        }
    }

    private static BedOperationalModel Create(string bedId) =>
        BedOperationalModel.Create(
            bedId,
            "B1",
            "K001",
            new OccupancyPolicyReff("POL-1", "Standard"));

    private static void EnsureSchema()
    {
        var options = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(options));
        conn.Open();

        ExecuteScriptWhenMissing(
            conn,
            "BILRG_RnaBedOperational",
            "BILRG_RnaBedOperational.sql");
        ExecuteScriptWhenMissing(
            conn,
            "BILRG_RnaBedReadinessTransaction",
            "BILRG_RnaBedReadinessTransaction.sql");
        ExecuteScriptWhenMissing(
            conn,
            "BILRG_RnaBedReadinessCorrection",
            "BILRG_RnaBedReadinessCorrection.sql");
    }

    private static void Cleanup(string bedId)
    {
        var options = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(options));
        conn.Open();
        conn.Execute(
            "DELETE FROM BILRG_RnaBedReadinessCorrection WHERE BedId = @BedId",
            new { BedId = bedId });
        conn.Execute(
            "DELETE FROM BILRG_RnaBedReadinessTransaction WHERE BedId = @BedId",
            new { BedId = bedId });
        conn.Execute(
            "DELETE FROM BILRG_RnaBedOperational WHERE BedId = @BedId",
            new { BedId = bedId });
    }

    private static void ExecuteScriptWhenMissing(
        SqlConnection conn,
        string tableName,
        string fileName)
    {
        var exists = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM sys.tables WHERE name = @TableName",
            new { TableName = tableName });
        if (exists != 0)
            return;

        var featurePath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "bilreg",
            "Bilreg.SqlDb",
            "BedUsageContext",
            "BedOperationalFeature");
        var script = File.ReadAllText(Path.Combine(featurePath, fileName));
        var batches = Regex.Split(
            script,
            @"^\s*GO\s*$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);
        foreach (var batch in batches.Where(x => !string.IsNullOrWhiteSpace(x)))
            conn.Execute(batch);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(
                    directory.FullName,
                    "src",
                    "bilreg",
                    "Bilreg.SqlDb")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root containing src/bilreg/Bilreg.SqlDb was not found.");
    }
}
