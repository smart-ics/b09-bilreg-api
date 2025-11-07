using System.Data;
using System.Data.SqlClient;
using System.Runtime.InteropServices.ComTypes;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BillContext.TindakanSub.TarifFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

// resharper disable inconsistentnaming
public interface IRegKomponenDal :
    IInsertBulk<RegKomponenDto>,
    IDelete<IRegKey>,
    IListData<RegKomponenDto, IRegKey>
{
}

public record RegKomponenDto(
    string fs_kd_reg,
    string fs_kd_detil_tarif,
    decimal fn_tarif,
    decimal fn_diskon,
    string fs_kd_petugas_medis,
    string fs_nm_detil_tarif,
    string fs_nm_petugas_medis)
{
    public static RegKomponenDto FromModel(string regId,RegKomponenType model)
    {
        var dto = new RegKomponenDto(regId, model.Komponen.KomponenId,
            model.Nilai, model.Diskon, model.PetugasMedis.PetugasMedisId,
            model.Komponen.KomponenName, model.PetugasMedis.PetugasMedisName);
        return dto;
    }

    public RegKomponenType ToModel()
    {
        var model = new RegKomponenType(
            new KomponenReff(fs_kd_detil_tarif, fs_nm_detil_tarif),
            new PetugasMedisReff(fs_kd_petugas_medis, fs_nm_petugas_medis),
            fn_tarif, fn_diskon);
        return model;
    }
}

public class RegKomponenDal : IRegKomponenDal
{
    private readonly DatabaseOptions _opt;

    public RegKomponenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<RegKomponenDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        conn.Open();
        bcp.AddMap("fs_kd_reg", "fs_kd_reg");
        bcp.AddMap("fs_kd_detil_tarif", "fs_kd_detil_tarif");
        bcp.AddMap("fn_tarif", "fn_tarif");
        bcp.AddMap("fn_diskon", "fn_diskon");
        bcp.AddMap("fs_kd_petugas_medis", "fs_kd_petugas_medis");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "ta_registrasi2";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IRegKey key)
    {
        const string sql = """
           DELETE FROM
               ta_registrasi2
           WHERE
               fs_kd_reg = @fs_kd_reg
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", key.RegId, SqlDbType.VarChar); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<RegKomponenDto> ListData(IRegKey filter)
    {
        const string sql = """
           SELECT
               aa.fs_kd_reg, aa.fs_kd_detil_tarif, aa.fn_tarif, 
               aa.fn_diskon, aa.fs_kd_petugas_medis,
               ISNULL(fs_nm_detil_tarif, '') fs_nm_detil_tarif,
               ISNULL(fs_nm_peg, '') AS fs_nm_petugas_medis
           FROM
               ta_registrasi2 aa
               LEFT JOIN ta_detil_tarif bb ON aa.fs_kd_detil_tarif = bb.fs_kd_detil_tarif
               LEFT JOIN td_peg cc ON aa.fs_kd_petugas_medis = cc.fs_kd_peg
           WHERE
               aa.fs_kd_reg = @fs_kd_reg
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", filter.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RegKomponenDto>(sql, dp);
    }
}

public class RegKomponenDalTest
{
    private readonly RegKomponenDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<RegKomponenDto> FakerList()
        => new List<RegKomponenDto>
        {
            new RegKomponenDto(
                fs_kd_reg: "A",
                fs_kd_detil_tarif: "B",
                fn_tarif: 1003m,
                fn_diskon: 102m,
                fs_kd_petugas_medis: "C",
                fs_nm_detil_tarif: "D",
                fs_nm_petugas_medis: "E"
            ),
            new RegKomponenDto(
                fs_kd_reg: "A",
                fs_kd_detil_tarif: "F",
                fn_tarif: 2001m,
                fn_diskon: 201m,
                fs_kd_petugas_medis: "G",
                fs_nm_detil_tarif: "H",
                fs_nm_petugas_medis: "I"
            )
        };

    private static IRegKey FakerKey()
        => RegModel.Key("A");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
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
        _sut.Insert(FakerList());
        var actual = _sut.ListData(FakerKey());
        actual.Should().BeEquivalentTo(FakerList(),
            opt => opt.Excluding(x => x.fs_nm_detil_tarif)
                .Excluding(x => x.fs_nm_petugas_medis));
    }
}
