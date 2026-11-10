using System.Data.SqlClient;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Infrastructure.AccountingContext.JurnalFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.AccountingContext.JurnalFeature;

public class Jurnal2DalTest
{
    private readonly Jurnal2Dal _sut = new(ConnStringHelper.GetTestEnv());

    public Jurnal2DalTest()
    {
        EnsureTableExists();
    }

    private static void EnsureTableExists()
    {
        const string sql = """
            IF OBJECT_ID('t_jurnal_dtl', 'U') IS NULL
            BEGIN
                CREATE TABLE t_jurnal_dtl
                (
                    fs_kd_jurnal VARCHAR(26) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_kd_jurnal DEFAULT(''),
                    fn_urut INT NOT NULL CONSTRAINT DF_t_jurnal_dtl_fn_urut DEFAULT(0),
                    fs_kd_rek VARCHAR(20) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_kd_rek DEFAULT(''),
                    fs_uraian VARCHAR(256) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_uraian DEFAULT(''),
                    fn_jurnald DECIMAL(18,2) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fn_jurnald DEFAULT(0),
                    fn_jurnalk DECIMAL(18,2) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fn_jurnalk DEFAULT(0),

                    fs_kd_unit VARCHAR(10) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_kd_unit DEFAULT(''),
                    fs_kd_jk VARCHAR(3) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_kd_jk DEFAULT(''),

                    fs_string00 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string00 DEFAULT(''),
                    fs_string01 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string01 DEFAULT(''),
                    fs_string02 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string02 DEFAULT(''),
                    fs_string03 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string03 DEFAULT(''),
                    fs_string04 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string04 DEFAULT(''),
                    fs_string05 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string05 DEFAULT(''),
                    fs_string06 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string06 DEFAULT(''),
                    fs_string07 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string07 DEFAULT(''),
                    fs_string08 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string08 DEFAULT(''),
                    fs_string09 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string09 DEFAULT(''),
                    fs_string10 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string10 DEFAULT(''),
                    fs_string11 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string11 DEFAULT(''),

                    fn_nilai_jasa DECIMAL(18,2) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fn_nilai_jasa DEFAULT(0),
                    fn_nilai_obat DECIMAL(18,2) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fn_nilai_obat DEFAULT(0),

                    CONSTRAINT PK_t_jurnal_dtl PRIMARY KEY CLUSTERED(fs_kd_jurnal, fn_urut)
                )
            END
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Execute(sql);
    }

    private static Jurnal2Dto Faker()
        => new Jurnal2Dto(
            fs_kd_jurnal: "JUR001",
            fn_urut: 1,
            fs_kd_rek: "REK001",
            fs_uraian: "Uraian Jurnal",
            fn_jurnald: 100000,
            fn_jurnalk: 0,
            fs_kd_unit: "UNIT001",
            fs_kd_jk: "JK1",
            fs_string00: "STR00",
            fs_string01: "STR01",
            fs_string02: "STR02",
            fs_string03: "STR03",
            fs_string04: "STR04",
            fs_string05: "STR05",
            fs_string06: "STR06",
            fs_string07: "STR07",
            fs_string08: "STR08",
            fs_string09: "STR09",
            fs_string10: "STR10",
            fs_string11: "STR11",
            fn_nilai_jasa: 50000,
            fn_nilai_obat: 50000
        );

    private static IJurnalKey FakerKey()
        => JurnalType.Default with { JurnalId = "JUR001" };

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var dtos = new List<Jurnal2Dto> { Faker() };
        _sut.Insert(dtos);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker();
        var dtos = new List<Jurnal2Dto> { dto };
        _sut.Insert(dtos);

        var actual = _sut.ListData(FakerKey());
        actual.Should().ContainEquivalentOf(dto);
    }
}
