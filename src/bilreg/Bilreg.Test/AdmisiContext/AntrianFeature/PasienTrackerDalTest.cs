using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class PasienTrackerDalTest
{
    private readonly IOptions<DatabaseOptions> _opt = ConnStringHelper.GetTestEnv();
    private readonly PasienTrackerDal _sut;

    public PasienTrackerDalTest()
    {
        _sut = new PasienTrackerDal(_opt);
    }

    /// <summary>
    /// Ensures StartPeriod/LastPeriod exist for F-12 additive persistence.
    /// Runs inside the ambient test transaction and rolls back with it when newly added.
    /// </summary>
    private void EnsureTrackingPeriodColumns()
    {
        const string sql = """
            IF COL_LENGTH('BILRG_PasienTracker', 'StartPeriod') IS NULL
            BEGIN
                ALTER TABLE BILRG_PasienTracker
                    ADD StartPeriod DATETIME NOT NULL
                        CONSTRAINT DF_BILRG_PasienTracker_StartPeriod DEFAULT('3000-01-01'),
                        LastPeriod DATETIME NOT NULL
                        CONSTRAINT DF_BILRG_PasienTracker_LastPeriod DEFAULT('3000-01-01');
            END
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt.Value));
        conn.Execute(sql);
    }

    private static PasienTrackerDto Faker()
        => new PasienTrackerDto(
            "A", "B",
            new DateTime(2025, 12, 1),
            new DateTime(2025, 12, 2),
            new DateTime(2025, 12, 1),
            new DateTime(2025, 12, 2));
    
    private static IPasienTrackerKey FakerKey()
        => PasienTrackerModel.Key("A");
    
    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        EnsureTrackingPeriodColumns();
        _sut.Insert(Faker());
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        EnsureTrackingPeriodColumns();
        _sut.Update(Faker());
    }

    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }
    
    [Fact]
    public void UT4_GetData()
    {
        using var trans = TransHelper.NewScope();
        EnsureTrackingPeriodColumns();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(Faker());
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        EnsureTrackingPeriodColumns();
        _sut.Insert(Faker());
        // Overlap: StartPeriod <= Tgl2 AND LastPeriod >= Tgl1 (BR-TRK-021)
        var actual = _sut.ListData(new Periode(new DateTime(2025, 12, 1), new DateTime(2025, 12, 3)));
        actual.Should().ContainEquivalentOf(Faker());
    }

    [Fact]
    public void UT6_ListData_WhenRelevantDateInsidePeriod_ThenReturned()
    {
        using var trans = TransHelper.NewScope();
        EnsureTrackingPeriodColumns();
        var dto = new PasienTrackerDto(
            "A", "B",
            new DateTime(2025, 1, 1),
            new DateTime(2025, 12, 15),
            new DateTime(2025, 12, 1),
            new DateTime(2025, 12, 20));
        _sut.Insert(dto);

        var actual = _sut.ListData(new Periode(new DateTime(2025, 12, 10), new DateTime(2025, 12, 10)));
        actual.Should().ContainEquivalentOf(dto);
    }
}
