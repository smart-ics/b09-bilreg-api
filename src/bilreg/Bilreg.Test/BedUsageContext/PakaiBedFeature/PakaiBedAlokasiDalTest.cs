using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedAlokasiDalTest
{
    private static readonly DateTime ProposedAt =
        new(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime StartedAt =
        new(2026, 7, 17, 8, 30, 0, DateTimeKind.Utc);

    private readonly PakaiBedAlokasiDal _sut =
        new(ConnStringHelper.GetTestEnv());

    [Fact]
    public void InsertAndGetData_StandardAllocation_MapsAllColumnsSuccessfully()
    {
        EnsureSchema();
        const string pakaiBedId = "PKBDAL000000001";
        Cleanup(pakaiBedId);
        try
        {
            var dto = CreateDto(pakaiBedId,
                physicalKelasId: "001",
                billingKelasId: "001",
                tipeKamarId: "1",
                tarif: 350000m,
                motherPakaiBedId: "",
                isTitipKelas: false,
                isNaikKelas: false);

            _sut.Insert(dto);
            var actual = _sut.GetData(PakaiBedAlokasiModel.Key(pakaiBedId));

            actual.Should().NotBeNull();
            actual.PakaiBedId.Should().Be(pakaiBedId);
            actual.Version.Should().Be(dto.Version);
            actual.RegId.Should().Be(dto.RegId);
            actual.PasienId.Should().Be(dto.PasienId);
            actual.BangsalId.Should().Be(dto.BangsalId);
            actual.KamarId.Should().Be(dto.KamarId);
            actual.BedId.Should().Be(dto.BedId);
            actual.WaitingListId.Should().Be(dto.WaitingListId);
            actual.RequestId.Should().Be(dto.RequestId);
            actual.PakaiBedPurpose.Should().Be(dto.PakaiBedPurpose);
            actual.OccupantRole.Should().Be(dto.OccupantRole);
            actual.PakaiBedStatus.Should().Be(dto.PakaiBedStatus);
            actual.AssignedBy.Should().Be(dto.AssignedBy);
            actual.BedAssignabilityEvidenceId.Should().Be(dto.BedAssignabilityEvidenceId);
            actual.PhysicalKelasId.Should().Be("001");
            actual.BillingKelasId.Should().Be("001");
            actual.TipeKamarId.Should().Be("1");
            actual.Tarif.Should().Be(350000m);
            actual.MotherPakaiBedId.Should().BeEmpty();
            actual.IsTitipKelas.Should().BeFalse();
            actual.IsNaikKelas.Should().BeFalse();
            actual.CrtUser.Should().Be(dto.CrtUser);
            actual.UpdUser.Should().Be(dto.UpdUser);
            actual.VodUser.Should().Be(dto.VodUser);
        }
        finally
        {
            Cleanup(pakaiBedId);
        }
    }

    [Fact]
    public void InsertAndGetData_DualClassTitipAndNaik_RoundTripsAccurately()
    {
        EnsureSchema();
        const string pakaiBedId = "PKBDAL000000002";
        Cleanup(pakaiBedId);
        try
        {
            var dto = CreateDto(pakaiBedId,
                physicalKelasId: "001",
                billingKelasId: "003",
                tipeKamarId: "2",
                tarif: 600000m,
                motherPakaiBedId: "",
                isTitipKelas: true,
                isNaikKelas: true);

            _sut.Insert(dto);
            var actual = _sut.GetData(PakaiBedAlokasiModel.Key(pakaiBedId));

            actual.Should().NotBeNull();
            actual.PhysicalKelasId.Should().Be("001");
            actual.BillingKelasId.Should().Be("003");
            actual.TipeKamarId.Should().Be("2");
            actual.Tarif.Should().Be(600000m);
            actual.IsTitipKelas.Should().BeTrue();
            actual.IsNaikKelas.Should().BeTrue();
            actual.MotherPakaiBedId.Should().BeEmpty();
        }
        finally
        {
            Cleanup(pakaiBedId);
        }
    }

    [Fact]
    public void InsertAndGetData_RoomingInBaby_PreservesMotherPakaiBedIdAndZeroTarif()
    {
        EnsureSchema();
        const string pakaiBedId = "PKBDAL000000003";
        Cleanup(pakaiBedId);
        try
        {
            var dto = CreateDto(pakaiBedId,
                physicalKelasId: "001",
                billingKelasId: "001",
                tipeKamarId: "1",
                tarif: 0m,
                motherPakaiBedId: "PKB-MOM-999",
                isTitipKelas: false,
                isNaikKelas: false);

            _sut.Insert(dto);
            var actual = _sut.GetData(PakaiBedAlokasiModel.Key(pakaiBedId));

            actual.Should().NotBeNull();
            actual.MotherPakaiBedId.Should().Be("PKB-MOM-999");
            actual.Tarif.Should().Be(0m);
            actual.IsTitipKelas.Should().BeFalse();
            actual.IsNaikKelas.Should().BeFalse();
        }
        finally
        {
            Cleanup(pakaiBedId);
        }
    }

    [Fact]
    public void Update_ModifiesNewColumns()
    {
        EnsureSchema();
        const string pakaiBedId = "PKBDAL000000004";
        Cleanup(pakaiBedId);
        try
        {
            var dto = CreateDto(pakaiBedId,
                physicalKelasId: "001",
                billingKelasId: "001",
                tipeKamarId: "1",
                tarif: 300000m,
                motherPakaiBedId: "",
                isTitipKelas: false,
                isNaikKelas: false);

            _sut.Insert(dto);

            var updatedDto = dto with
            {
                BillingKelasId = "002",
                Tarif = 450000m,
                IsTitipKelas = true,
                IsNaikKelas = true,
                MotherPakaiBedId = "PKB-MOTHER-X",
                Version = 2,
                UpdUser = "UPD-USER-1"
            };

            _sut.Update(updatedDto);

            var actual = _sut.GetData(PakaiBedAlokasiModel.Key(pakaiBedId));
            actual.BillingKelasId.Should().Be("002");
            actual.Tarif.Should().Be(450000m);
            actual.IsTitipKelas.Should().BeTrue();
            actual.IsNaikKelas.Should().BeTrue();
            actual.MotherPakaiBedId.Should().Be("PKB-MOTHER-X");
            actual.Version.Should().Be(2);
            actual.UpdUser.Should().Be("UPD-USER-1");
        }
        finally
        {
            Cleanup(pakaiBedId);
        }
    }

    [Fact]
    public void UpdateConditional_MatchingVersion_SucceedsAndIncrementsVersion()
    {
        EnsureSchema();
        const string pakaiBedId = "PKBDAL000000005";
        Cleanup(pakaiBedId);
        try
        {
            var dto = CreateDto(pakaiBedId,
                physicalKelasId: "001",
                billingKelasId: "001",
                tipeKamarId: "1",
                tarif: 300000m,
                motherPakaiBedId: "",
                isTitipKelas: false,
                isNaikKelas: false);

            _sut.Insert(dto);

            var update = dto with
            {
                Version = 2,
                BillingKelasId = "002",
                Tarif = 500000m
            };

            var affected = _sut.UpdateConditional(update, expectedVersion: 1);
            var staleAffected = _sut.UpdateConditional(update with { Version = 3 }, expectedVersion: 1);

            affected.Should().Be(1);
            staleAffected.Should().Be(0);

            var actual = _sut.GetData(PakaiBedAlokasiModel.Key(pakaiBedId));
            actual.Version.Should().Be(2);
            actual.BillingKelasId.Should().Be("002");
            actual.Tarif.Should().Be(500000m);
        }
        finally
        {
            Cleanup(pakaiBedId);
        }
    }

    private static void EnsureSchema()
    {
        var options = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(options));
        conn.Open();
        ExecuteScriptWhenMissing(
            conn,
            "BILRG_RnaPakaiBedAlokasi",
            "BILRG_RnaPakaiBedAlokasi.sql");
        ExecuteScript(
            conn,
            "BILRG_RnaPakaiBedAlokasi_Alter.sql");
    }

    private static void Cleanup(string pakaiBedId)
    {
        var options = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(options));
        conn.Open();
        conn.Execute(
            "DELETE FROM BILRG_RnaPakaiBedAlokasi WHERE PakaiBedId = @PakaiBedId",
            new { PakaiBedId = pakaiBedId });
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

        ExecuteScript(conn, fileName);
    }

    private static void ExecuteScript(
        SqlConnection conn,
        string fileName)
    {
        var featurePath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "bilreg",
            "Bilreg.SqlDb",
            "BedUsageContext",
            "PakaiBedFeature");
        var scriptPath = Path.Combine(featurePath, fileName);
        if (!File.Exists(scriptPath))
            return;

        var script = File.ReadAllText(scriptPath);
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

    private static PakaiBedAlokasiDto CreateDto(
        string pakaiBedId,
        string physicalKelasId = "",
        string billingKelasId = "",
        string tipeKamarId = "",
        decimal tarif = 0,
        string motherPakaiBedId = "",
        bool isTitipKelas = false,
        bool isNaikKelas = false) =>
        new(
            PakaiBedId: pakaiBedId,
            Version: 1,
            RegId: "REG-DAL-01",
            PasienId: "PAS-DAL-01",
            BangsalId: "BG",
            KamarId: "KMR01",
            BedId: "BED01",
            WaitingListId: "WTL01",
            RequestId: $"REQ-{pakaiBedId}",
            PakaiBedPurpose: (int)PakaiBedPurposeEnum.Clinical,
            OccupantRole: (int)OccupantRoleEnum.Primary,
            PakaiBedStatus: (int)PakaiBedStatusEnum.Active,
            ProposedAt: ProposedAt,
            StartedAt: StartedAt,
            AssignedBy: "NURSE-01",
            BedAssignabilityEvidenceId: "EVI-01",
            PhysicalKelasId: physicalKelasId,
            BillingKelasId: billingKelasId,
            TipeKamarId: tipeKamarId,
            Tarif: tarif,
            MotherPakaiBedId: motherPakaiBedId,
            IsTitipKelas: isTitipKelas,
            IsNaikKelas: isNaikKelas,
            CrtUser: "SYS",
            CrtDate: ProposedAt,
            UpdUser: "SYS",
            UpdDate: ProposedAt,
            VodUser: string.Empty,
            VodDate: new DateTime(3000, 1, 1));
}
