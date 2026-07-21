using System.Data.SqlClient;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianDalTest
{
    private readonly IOptions<DatabaseOptions> _opt = ConnStringHelper.GetTestEnv();
    private readonly AntrianDal _sut;

    public AntrianDalTest()
    {
        _sut = new AntrianDal(_opt);
    }

    private static AntrianDto Faker() 
        => new AntrianDto("A", new DateTime(2025, 10, 21), "B", "C", "D", "E", "SP");

    /// <summary>
    /// Ensures ServicePointCode exists for F-06 additive persistence.
    /// Runs inside the ambient test transaction and rolls back with it when newly added.
    /// </summary>
    private void EnsureServicePointCodeColumn()
    {
        const string sql = """
            IF COL_LENGTH('BILRG_Antrian', 'ServicePointCode') IS NULL
            BEGIN
                ALTER TABLE BILRG_Antrian
                    ADD ServicePointCode VARCHAR(50) NOT NULL
                        CONSTRAINT DF_BILRG_Antrian_ServicePointCode DEFAULT('');
            END
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt.Value));
        conn.Execute(sql);
    }
    
    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        EnsureServicePointCodeColumn();
        _sut.Insert(Faker());
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        EnsureServicePointCodeColumn();
        _sut.Insert(Faker());
        _sut.Update(Faker());
    }

    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(Faker());
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        EnsureServicePointCodeColumn();
        _sut.Insert(Faker());
        var actual = _sut.GetData(Faker());
        actual.Should().BeEquivalentTo(Faker());
    }
    
    [Fact]
    public void UT5_ListDataTest()
    {
        var periode = new Periode(new DateTime(2025, 10, 21));
        using var trans = TransHelper.NewScope();
        EnsureServicePointCodeColumn();
        _sut.Insert(Faker());
        var actual = _sut.ListData(periode);
        actual.Should().ContainEquivalentOf(Faker());
    }
}
