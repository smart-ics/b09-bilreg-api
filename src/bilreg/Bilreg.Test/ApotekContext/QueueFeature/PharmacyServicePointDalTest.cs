using System.Data.SqlClient;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class PharmacyServicePointDalTest
{
    private readonly PharmacyServicePointDal _dal;
    private readonly PharmacyServicePointRepo _repo;

    public PharmacyServicePointDalTest()
    {
        var opt = ConnStringHelper.GetTestEnv();
        _dal = new PharmacyServicePointDal(opt);
        _repo = new PharmacyServicePointRepo(_dal);
    }

    private static string NewId(string prefix = "SP") => $"{prefix}{Guid.NewGuid().ToString("N")[..8]}";

    [Fact]
    public void Dal_Insert_And_GetData_PersistsAndRetrievesAllColumns()
    {
        var spId = NewId();
        var depoId = NewId("DEP");
        var now = new DateTime(2026, 10, 18, 10, 0, 0);

        Cleanup(spId);

        try
        {
            var dto = new PharmacyServicePointDto(
                spId,
                depoId,
                "Loket Racikan 1",
                "B",
                Active: true,
                CrtUser: "usr1",
                CrtDate: now,
                UpdUser: "usr1",
                UpdDate: now,
                VodUser: "",
                VodDate: new DateTime(3000, 1, 1));

            _dal.Insert(dto);

            var actual = _dal.GetData(PharmacyServicePointModel.Key(spId));
            actual.Should().NotBeNull();
            actual.ServicePointId.Should().Be(spId);
            actual.DepoFarmasiId.Should().Be(depoId);
            actual.DisplayName.Should().Be("Loket Racikan 1");
            actual.QueuePrefix.Should().Be("B");
            actual.Active.Should().BeTrue();
            actual.CrtUser.Should().Be("usr1");
            actual.CrtDate.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
            actual.VodUser.Should().BeEmpty();
            actual.VodDate.Date.Should().Be(new DateTime(3000, 1, 1));

            var actualById = _dal.Get(spId);
            actualById.Should().NotBeNull();
            actualById!.ServicePointId.Should().Be(spId);
        }
        finally
        {
            Cleanup(spId);
        }
    }

    [Fact]
    public void Dal_Update_ModifiesColumnsCorrectly()
    {
        var spId = NewId();
        var depoId = NewId("DEP");
        var now = DateTime.Now;

        Cleanup(spId);

        try
        {
            var dto = new PharmacyServicePointDto(
                spId,
                depoId,
                "Layanan Awal",
                "A",
                Active: true,
                CrtUser: "usr1",
                CrtDate: now,
                UpdUser: "usr1",
                UpdDate: now,
                VodUser: "",
                VodDate: new DateTime(3000, 1, 1));
            _dal.Insert(dto);

            var updatedDto = dto with
            {
                DisplayName = "Layanan Diperbarui",
                QueuePrefix = "C",
                Active = false,
                UpdUser = "usr2",
                UpdDate = now.AddMinutes(5)
            };
            _dal.Update(updatedDto);

            var actual = _dal.GetData(PharmacyServicePointModel.Key(spId));
            actual.DisplayName.Should().Be("Layanan Diperbarui");
            actual.QueuePrefix.Should().Be("C");
            actual.Active.Should().BeFalse();
            actual.UpdUser.Should().Be("usr2");
        }
        finally
        {
            Cleanup(spId);
        }
    }

    [Fact]
    public void Dal_ListActive_And_ListByDepo_ReturnsMatchingRecords()
    {
        var spId1 = NewId();
        var spId2 = NewId();
        var depoId = NewId("DEP");
        var now = DateTime.Now;

        Cleanup(spId1);
        Cleanup(spId2);

        try
        {
            _dal.Insert(new PharmacyServicePointDto(
                spId1, depoId, "Layanan Aktif", "A", Active: true,
                "u", now, "u", now, "", new DateTime(3000, 1, 1)));
            _dal.Insert(new PharmacyServicePointDto(
                spId2, depoId, "Layanan Nonaktif", "B", Active: false,
                "u", now, "u", now, "", new DateTime(3000, 1, 1)));

            var activeList = _dal.ListActive().ToList();
            activeList.Should().Contain(x => x.ServicePointId == spId1);
            activeList.Should().NotContain(x => x.ServicePointId == spId2);

            var depoList = _dal.ListByDepo(depoId).ToList();
            depoList.Should().HaveCount(2);
            depoList.Should().Contain(x => x.ServicePointId == spId1);
            depoList.Should().Contain(x => x.ServicePointId == spId2);
        }
        finally
        {
            Cleanup(spId1);
            Cleanup(spId2);
        }
    }

    [Fact]
    public void Repo_RoundTrip_Creates_Updates_And_Loads_DomainModel()
    {
        var spId = NewId();
        var depoId = NewId("DEP");

        Cleanup(spId);

        try
        {
            // Create & Save
            var model = PharmacyServicePointModel.Create(spId, depoId, "Poli Apotek", "A");
            _repo.SaveChanges(model);

            // LoadEntity
            var loadedMaybe = _repo.LoadEntity(PharmacyServicePointModel.Key(spId));
            loadedMaybe.HasValue.Should().BeTrue();
            var loaded = loadedMaybe.Value;
            loaded.ServicePointId.Should().Be(spId);
            loaded.DepoFarmasiId.Should().Be(depoId);
            loaded.DisplayName.Should().Be("Poli Apotek");
            loaded.QueuePrefix.Should().Be("A");
            loaded.Active.Should().BeTrue();

            // Mutate model: Deactivate & Update
            loaded.Deactivate();
            loaded.Update("Poli Apotek 2", "B");
            _repo.SaveChanges(loaded);

            var reloaded = _repo.LoadEntity(PharmacyServicePointModel.Key(spId)).Value;
            reloaded.Active.Should().BeFalse();
            reloaded.DisplayName.Should().Be("Poli Apotek 2");
            reloaded.QueuePrefix.Should().Be("B");

            // ListByDepo through repo
            var byDepo = _repo.ListByDepo(depoId);
            byDepo.Should().ContainSingle(x => x.ServicePointId == spId);
        }
        finally
        {
            Cleanup(spId);
        }
    }

    private static void Cleanup(string servicePointId)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("DELETE FROM BILRG_AptServicePoint WHERE ServicePointId = @servicePointId", new { servicePointId });
    }
}
