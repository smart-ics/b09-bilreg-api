using System.Data.SqlClient;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Infrastructure.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class QueueMappingDalTest
{
    private readonly QueueMappingDal _dal;
    private readonly QueueMappingRepo _repo;

    public QueueMappingDalTest()
    {
        var opt = ConnStringHelper.GetTestEnv();
        _dal = new QueueMappingDal(opt);
        _repo = new QueueMappingRepo(_dal);
    }

    private static string NewId(string prefix = "D") => $"{prefix}{Guid.NewGuid().ToString("N")[..8]}";

    [Fact]
    public void Dal_Insert_And_GetData_PersistsPasienHadirColumns()
    {
        var demandId = NewId("RX");
        var antrianId = NewId("ANT");
        const int noUrut = 1;
        var now = DateTime.Now;

        Cleanup((int)QueueDemandKindEnum.ResepKerja, demandId);

        try
        {
            var dto = new QueueMappingDto(
                DemandKind: (int)QueueDemandKindEnum.ResepKerja,
                DemandId: demandId,
                AntrianId: antrianId,
                NoUrut: noUrut,
                PasienTrackerId: "TRK1",
                PasienHadir: false,
                PasienHadirAt: ApotekDate.Empty,
                MappingMethod: (int)QueueMappingMethodEnum.Manual,
                MappedBy: "usr1",
                MappedAt: now,
                CrtUser: "usr1",
                CrtDate: now,
                UpdUser: "usr1",
                UpdDate: now);

            _dal.Insert(dto);

            var actual = _dal.GetData(QueueMappingModel.Key(QueueDemandKindEnum.ResepKerja, demandId));
            actual.Should().NotBeNull();
            actual.DemandId.Should().Be(demandId);
            actual.AntrianId.Should().Be(antrianId);
            actual.NoUrut.Should().Be(noUrut);
            actual.PasienHadir.Should().BeFalse();
            actual.PasienHadirAt.Date.Should().Be(new DateTime(3000, 1, 1));
        }
        finally
        {
            Cleanup((int)QueueDemandKindEnum.ResepKerja, demandId);
        }
    }

    [Fact]
    public void Dal_SetPasienHadir_UpdatesFlagAndTimestamp()
    {
        var demandId = NewId("RX");
        var antrianId = NewId("ANT");
        const int noUrut = 1;
        var now = DateTime.Now;

        Cleanup((int)QueueDemandKindEnum.ResepKerja, demandId);

        try
        {
            var dto = new QueueMappingDto(
                (int)QueueDemandKindEnum.ResepKerja, demandId, antrianId, noUrut, "TRK1",
                false, ApotekDate.Empty, (int)QueueMappingMethodEnum.Manual,
                "usr1", now, "usr1", now, "usr1", now);
            _dal.Insert(dto);

            var hadirAt = now.AddMinutes(10);
            var success = _dal.SetPasienHadir((int)QueueDemandKindEnum.ResepKerja, demandId, "usr_loket", hadirAt);
            success.Should().BeTrue();

            var actual = _dal.GetData(QueueMappingModel.Key(QueueDemandKindEnum.ResepKerja, demandId));
            actual.PasienHadir.Should().BeTrue();
            actual.PasienHadirAt.Should().BeCloseTo(hadirAt, TimeSpan.FromSeconds(1));
            actual.UpdUser.Should().Be("usr_loket");
        }
        finally
        {
            Cleanup((int)QueueDemandKindEnum.ResepKerja, demandId);
        }
    }

    [Fact]
    public void Repo_RoundTrip_With_PasienHadir_PreservesValues()
    {
        var demandId = NewId("RX");
        var antrianId = NewId("ANT");
        const int noUrut = 3;
        var now = DateTime.Now;

        Cleanup((int)QueueDemandKindEnum.ResepKerja, demandId);

        try
        {
            var model = QueueMappingModel.Create(
                QueueDemandKindEnum.ResepKerja, demandId, antrianId, noUrut, "TRK1",
                QueueMappingMethodEnum.Manual, "usr1", now);
            _repo.SaveChanges(model);

            // Load and check initial
            var loaded = _repo.LoadEntity(QueueMappingModel.Key(QueueDemandKindEnum.ResepKerja, demandId)).Value;
            loaded.PasienHadir.Should().BeFalse();

            // Mark hadir via Repo
            var hadirAt = now.AddMinutes(5);
            var updated = _repo.SetPasienHadir(QueueDemandKindEnum.ResepKerja, demandId, "usr_nurse", hadirAt);
            updated.Should().BeTrue();

            // Reload and verify
            var reloaded = _repo.LoadEntity(QueueMappingModel.Key(QueueDemandKindEnum.ResepKerja, demandId)).Value;
            reloaded.PasienHadir.Should().BeTrue();
            reloaded.PasienHadirAt.Should().BeCloseTo(hadirAt, TimeSpan.FromSeconds(1));

            // ListByQueue
            var byQueue = _repo.ListByQueue(antrianId, noUrut);
            byQueue.Should().ContainSingle(x => x.DemandId == demandId && x.PasienHadir);
        }
        finally
        {
            Cleanup((int)QueueDemandKindEnum.ResepKerja, demandId);
        }
    }

    private static void Cleanup(int demandKind, string demandId)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("DELETE FROM BILRG_AptQueueMapping WHERE DemandKind = @demandKind AND DemandId = @demandId",
            new { demandKind, demandId });
    }
}
