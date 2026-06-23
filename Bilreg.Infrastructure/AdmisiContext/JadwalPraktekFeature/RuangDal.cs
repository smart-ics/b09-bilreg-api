using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;

public interface IRuangDal :
    IInsert<RuangDto>,
    IUpdate<RuangDto>,
    IDelete<IRuangKey>,
    IGetData<RuangDto, IRuangKey>,
    IListData<RuangDto>
{ }
public class RuangDal :IRuangDal
{
    private readonly DatabaseOptions _opt;

    public RuangDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public void Insert(RuangDto dto)
    {
        const string sql = """
            INSERT INTO HiDok_Ruang(
               RuangId, RuangName, PrefixAntrian)
            VALUES (
               @RuangId, @RuangName, @PrefixAntrian)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RuangId", dto.RuangId, SqlDbType.VarChar);
        dp.AddParam("@RuangName", dto.RuangName, SqlDbType.VarChar);
        dp.AddParam("@PrefixAntrian", dto.PrefixAntrian, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(RuangDto dto)
    {
        const string sql = """
            UPDATE HiDok_Ruang
            SET
                RuangName = @RuangName, 
                PrefixAntrian = @PrefixAntrian
            WHERE
               RuangId = @RuangId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RuangId", dto.RuangId, SqlDbType.VarChar);
        dp.AddParam("@RuangName", dto.RuangName, SqlDbType.VarChar);
        dp.AddParam("@PrefixAntrian", dto.PrefixAntrian, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IRuangKey key)
    {
        const string sql = """
            DELETE FROM HiDok_Ruang 
            WHERE RuangId = @RuangId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RuangId", key.RuangId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public RuangDto GetData(IRuangKey key)
    {
        const string sql = """
            SELECT
               aa.RuangId, aa.RuangName, aa.PrefixAntrian
            FROM 
               HiDok_Ruang aa
            WHERE
               aa.RuangId = @RuangId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RuangId", key.RuangId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<RuangDto>(sql, dp);
    }

    public IEnumerable<RuangDto> ListData()
    {
        const string sql = """
            SELECT
               aa.RuangId, aa.RuangName, aa.PrefixAntrian
            FROM 
               HiDok_Ruang aa
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RuangDto>(sql);
    }
}


