//  resharper disable inconsistentnaming

using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public interface IRegJaminanDal :
    IInsert<RegJaminanDto>,
    IUpdate<RegJaminanDto>,
    IDelete<IRegKey>,
    IGetData<RegJaminanDto, IRegKey>
{
}

public record RegJaminanDto(
    string fs_kd_reg,
    string fs_kd_polis,
    string fs_no_polis,
    string fs_atas_nama)
{
    public static RegJaminanDto FromModel(RegModel model)
    {
        var result = new RegJaminanDto(
            model.RegId, model.Polis.PolisId,
            model.Polis.NoPolis, model.Polis.AtasName);
        return result;
    }
}
public class RegJaminanDal : IRegJaminanDal
{
    private readonly DatabaseOptions _opt;

    public RegJaminanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(RegJaminanDto model)
    {
        const string sql = """
           INSERT INTO ta_reg_jaminan(fs_kd_reg, fs_kd_polis)
           VALUES (@fs_kd_reg, @fs_kd_polis)
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", model.fs_kd_reg, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_polis", model.fs_kd_polis, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(RegJaminanDto model)
    {
        const string sql = """
           UPDATE ta_reg_jaminan
           SET fs_kd_polis = @fs_kd_polis
           WHERE fs_kd_reg = @fs_kd_reg
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", model.fs_kd_reg, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_polis", model.fs_kd_polis, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IRegKey key)
    {
        const string sql = """
           DELETE FROM ta_reg_jaminan
           WHERE fs_kd_reg = @fs_kd_reg
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", key.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public RegJaminanDto GetData(IRegKey key)
    {
        const string sql = """
           SELECT
                aa.fs_kd_reg, aa.fs_kd_polis,
                ISNULL(bb.fs_no_polis,'') fs_no_polis,
                ISNULL(bb.fs_atas_nama, '') fs_atas_nama
           FROM
                ta_reg_jaminan aa
                LEFT JOIN ta_polis bb ON aa.fs_kd_polis = bb.fs_kd_polis
           WHERE
                fs_kd_reg = @fs_kd_reg
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", key.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<RegJaminanDto>(sql, dp);
    }
}

public class RegJaminanDalTest
{
    private readonly RegJaminanDal _sut = new(ConnStringHelper.GetTestEnv());

    private static RegJaminanDto Faker()
        => new RegJaminanDto(
            fs_kd_reg: "A",
            fs_kd_polis: "B",
            fs_no_polis: "C",
            fs_atas_nama: "D"
        );

    private static IRegKey FakerKey()
        => RegModel.Key("A");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeEquivalentTo(Faker(), 
            opt => opt.Excluding(x => x.fs_no_polis)
                .Excluding(x => x.fs_atas_nama));
    }
}
