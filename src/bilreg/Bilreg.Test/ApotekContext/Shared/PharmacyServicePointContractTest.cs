using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ApotekContext.Shared;

public class PharmacyServicePointContractTest
{
    private const string PharmacyServicePointId = "APT";
    private const string PharmacyDisplayName = "Apotek Rawat Jalan (example)";
    private const string PharmacyQueuePrefix = "P";

    [Fact]
    public void Example_seed_targets_adm_service_point_with_pharmacy_id()
    {
        var sqlRoot = FindApotekSqlRoot();
        var seed = File.ReadAllText(Path.Combine(sqlRoot, "BILRG_Apt_Seed_ServicePoint.example.sql"));

        seed.Should().Contain("BILRG_AdmServicePoint");
        seed.Should().Contain($"ServicePointId = '{PharmacyServicePointId}'");
    }

    [Fact]
    public void LoadEntity_Resolves_pharmacy_service_point_through_admission_repo()
    {
        EnsureAdmServicePointSchema();
        var repo = CreateRepo();

        SeedPharmacyServicePoint(repo);

        var loaded = repo.LoadEntity(AdmissionServicePointModel.Key(PharmacyServicePointId));
        loaded.HasValue.Should().BeTrue();
        loaded.Value.ServicePointId.Should().Be(PharmacyServicePointId);
        loaded.Value.DisplayName.Should().Be(PharmacyDisplayName);
        loaded.Value.QueuePrefix.Should().Be(PharmacyQueuePrefix);
        loaded.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void EnsureAdmissionQueue_Accepts_pharmacy_service_point()
    {
        EnsureAdmServicePointSchema();
        var repo = CreateRepo();
        SeedPharmacyServicePoint(repo);

        var resolver = new AdmissionServicePointResolver(repo);
        var queue = CreateQueue(PharmacyServicePointId);

        var act = () => resolver.EnsureAdmissionQueue(queue);

        act.Should().NotThrow();
    }

    private static AdmissionServicePointRepo CreateRepo()
    {
        var db = ConnStringHelper.GetTestEnv();
        return new AdmissionServicePointRepo(new AdmissionServicePointDal(db));
    }

    private static void SeedPharmacyServicePoint(AdmissionServicePointRepo repo)
    {
        var existing = repo.LoadEntity(AdmissionServicePointModel.Key(PharmacyServicePointId));
        if (existing.HasValue)
        {
            if (!existing.Value.IsActive
                || existing.Value.DisplayName != PharmacyDisplayName
                || existing.Value.QueuePrefix != PharmacyQueuePrefix)
            {
                repo.SaveChanges(AdmissionServicePointModel.Create(
                    PharmacyServicePointId,
                    PharmacyDisplayName,
                    PharmacyQueuePrefix));
            }

            return;
        }

        repo.SaveChanges(AdmissionServicePointModel.Create(
            PharmacyServicePointId,
            PharmacyDisplayName,
            PharmacyQueuePrefix));
    }

    private static AntrianModel CreateQueue(string servicePointId)
    {
        var sequencer = new Mock<ISequencer>();
        return new AntrianModel(
            $"Q-{servicePointId}",
            new DateOnly(2026, 7, 27),
            TimeOnly.MinValue,
            TimeOnly.MaxValue,
            $"tag-{servicePointId}",
            servicePointId,
            new ServicePointType(servicePointId, servicePointId),
            [],
            sequencer.Object);
    }

    private static void EnsureAdmServicePointSchema()
    {
        var options = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(options));
        conn.Open();

        var exists = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM sys.tables WHERE name = 'BILRG_AdmServicePoint'");
        if (exists != 0)
            return;

        var scriptPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "bilreg",
            "Bilreg.SqlDb",
            "AdmisiContext",
            "AntrianFeature",
            "BILRG_AdmServicePoint.sql");
        var script = File.ReadAllText(scriptPath);
        var batches = Regex.Split(
            script,
            @"^\s*GO\s*$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);
        foreach (var batch in batches.Where(x => !string.IsNullOrWhiteSpace(x)))
            conn.Execute(batch);
    }

    private static string FindApotekSqlRoot()
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
