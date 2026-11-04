using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class ScheduleOpHistDalTest
{
    private readonly ScheduleOpHistDal _sut = new(ConnStringHelper.GetTestEnv());

    static ScheduleOpHistDalTest()
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        const string sql1 = """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_ScheduleOpHist') AND name = 'ChangeType')
                ALTER TABLE BILRG_ScheduleOpHist
                    ADD ChangeType VARCHAR(20) NOT NULL
                    CONSTRAINT DF_BILRG_ScheduleOpHist_ChangeType DEFAULT('RESCHEDULE');
            """;
        const string sql2 = """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('BILRG_ScheduleOpPpa') AND name = 'IX_BILRG_ScheduleOpPpa_PpaId')
                CREATE INDEX IX_BILRG_ScheduleOpPpa_PpaId
                    ON BILRG_ScheduleOpPpa (PpaId)
                    INCLUDE (ScheduleOpId, ProfesiId)
                    WITH (FILLFACTOR = 95);
            """;
        conn.Execute(sql1);
        conn.Execute(sql2);
    }

    private static ScheduleOpHistDto Faker(int noUrut = 1, string changeType = "RESCHEDULE")
        => new ScheduleOpHistDto(
            $"HIST_{noUrut}",
            "SCHED_01",
            noUrut,
            new DateTime(2026, 10, 20, 8, 0, 0),
            new DateTime(2026, 10, 20, 10, 0, 0),
            120,
            150,
            "OK_01",
            "OK_02",
            "DOKTER_01",
            "DOKTER_02",
            "Pergeseran jadwal karena ruang OK disterilisasi",
            true,
            "Disetujui kepala bedah",
            "USER_TEST",
            new DateTime(2026, 10, 19, 14, 30, 0),
            changeType);

    private static IScheduleOpKey FakerKey()
        => ScheduleOpModel.Key("SCHED_01");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var item = Faker(1, "ASSIGN_LEADER");
        _sut.Insert(item);

        var actual = _sut.ListData(FakerKey()).ToList();
        actual.Should().ContainSingle();
        actual[0].ChangeType.Should().Be("ASSIGN_LEADER");
        actual[0].Should().BeEquivalentTo(item);
    }

    [Fact]
    public void InsertBulkTest()
    {
        using var trans = TransHelper.NewScope();
        var list = new List<ScheduleOpHistDto>
        {
            Faker(1, "RESCHEDULE"),
            Faker(2, "ASSIGN_LEADER")
        };
        _sut.Insert(list);

        var actual = _sut.ListData(FakerKey()).ToList();
        actual.Should().HaveCount(2);
        actual[0].ChangeType.Should().Be("RESCHEDULE");
        actual[1].ChangeType.Should().Be("ASSIGN_LEADER");
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker(1));
        _sut.Delete(FakerKey());
        var actual = _sut.ListData(FakerKey());
        actual.Should().BeEmpty();
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var item1 = Faker(1, "RESCHEDULE");
        var item2 = Faker(2, "ASSIGN_LEADER");
        _sut.Insert(item2);
        _sut.Insert(item1);

        var actual = _sut.ListData(FakerKey()).ToList();

        actual.Should().HaveCount(2);
        actual[0].Should().BeEquivalentTo(item1);
        actual[1].Should().BeEquivalentTo(item2);
        actual[0].ChangeType.Should().Be("RESCHEDULE");
        actual[1].ChangeType.Should().Be("ASSIGN_LEADER");
        actual.Should().BeInAscendingOrder(x => x.NoUrut);
    }
}
