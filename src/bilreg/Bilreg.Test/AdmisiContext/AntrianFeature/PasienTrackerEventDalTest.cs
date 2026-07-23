using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public class PasienTrackerEventDalTest
{
    private readonly IOptions<DatabaseOptions> _opt = ConnStringHelper.GetTestEnv();
    private readonly PasienTrackerEventDal _sut;

    public PasienTrackerEventDalTest()
    {
        _sut = new PasienTrackerEventDal(_opt);
    }

    private static PasienTrackerEventDto Faker()
        => new PasienTrackerEventDto("A", 2, "B", new DateTime(2025, 1, 2), "C");
    private static IPasienTrackerKey FakerKey()
        => PasienTrackerModel.Key("A");

    /// <summary>
    /// Ensures PK is (PasienTrackerId, NoUrut) so equal EventDate rows can be inserted.
    /// Runs inside the ambient test transaction and rolls back with it.
    /// </summary>
    private void EnsureAppendOnlyPrimaryKey()
    {
        const string sql = """
            IF EXISTS (
                SELECT 1
                FROM sys.key_constraints kc
                INNER JOIN sys.index_columns ic
                    ON ic.object_id = kc.parent_object_id
                   AND ic.index_id = kc.unique_index_id
                INNER JOIN sys.columns c
                    ON c.object_id = ic.object_id
                   AND c.column_id = ic.column_id
                WHERE kc.name = 'PK_BILRG_PasienTrackerEvent'
                  AND c.name = 'EventDate'
            )
            BEGIN
                ALTER TABLE BILRG_PasienTrackerEvent
                    DROP CONSTRAINT PK_BILRG_PasienTrackerEvent;

                ALTER TABLE BILRG_PasienTrackerEvent
                    ADD CONSTRAINT PK_BILRG_PasienTrackerEvent
                    PRIMARY KEY CLUSTERED (PasienTrackerId, NoUrut);
            END
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt.Value));
        conn.Execute(sql);
    }
    
    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }

    [Fact]
    public void UT2_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData("A", 2);
        actual.Should().BeEquivalentTo(Faker());
    }

    [Fact]
    public void UT3_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(FakerKey());
        actual.Should().ContainEquivalentOf(Faker());
    }

    [Fact]
    public void UT4_ListData_OrdersByEventDateThenNoUrut()
    {
        using var trans = TransHelper.NewScope();
        EnsureAppendOnlyPrimaryKey();

        var day1 = new PasienTrackerEventDto("A", 2, "FIRST", new DateTime(2025, 1, 1, 10, 0, 0), "R1");
        var day2a = new PasienTrackerEventDto("A", 3, "SECOND", new DateTime(2025, 1, 2, 10, 0, 0), "R2");
        var day2b = new PasienTrackerEventDto("A", 4, "THIRD", new DateTime(2025, 1, 2, 10, 0, 0), "R3");
        var day3 = new PasienTrackerEventDto("A", 1, "FOURTH", new DateTime(2025, 1, 3, 10, 0, 0), "R4");

        _sut.Insert(day3);
        _sut.Insert(day2b);
        _sut.Insert(day1);
        _sut.Insert(day2a);

        var actual = _sut.ListData(FakerKey())!.ToList();

        actual.Select(x => x.EventName).Should().Equal("FIRST", "SECOND", "THIRD", "FOURTH");
        actual.Select(x => x.NoUrut).Should().Equal(2, 3, 4, 1);
    }

    [Fact]
    public void UT5_Insert_AllowsEqualEventDateWithDistinctNoUrut()
    {
        using var trans = TransHelper.NewScope();
        EnsureAppendOnlyPrimaryKey();

        var occurredAt = new DateTime(2025, 1, 2, 10, 0, 0);
        var first = new PasienTrackerEventDto("A", 1, "E1", occurredAt, "R1");
        var second = new PasienTrackerEventDto("A", 2, "E2", occurredAt, "R2");

        _sut.Insert(first);
        _sut.Insert(second);

        var actual = _sut.ListData(FakerKey())!.ToList();
        actual.Should().HaveCount(2);
        actual.Should().ContainEquivalentOf(first);
        actual.Should().ContainEquivalentOf(second);
    }
}
