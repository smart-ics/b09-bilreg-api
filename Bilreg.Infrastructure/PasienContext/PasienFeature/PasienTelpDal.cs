using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public interface IPasienTelpDal :
    IInsert<PasienTelpDto>,
    IUpdate<PasienTelpDto, string>, 
    IDelete<IPasienKey>,
    IListData<PasienTelpDto, IPasienKey>
{ }
public class PasienTelpDal : IPasienTelpDal
{
    private readonly DatabaseOptions _opt;

    public PasienTelpDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public void Insert(PasienTelpDto dto)
    {
        const string sql = """
            INSERT INTO tc_mr_telp(
               fs_mr, fs_kd_jenis_telp, fs_no_telp, fb_default)
            VALUES(
               @fs_mr, @fs_kd_jenis_telp, @fs_no_telp, @fb_default)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", dto.fs_mr, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_jenis_telp", dto.fs_kd_jenis_telp, SqlDbType.VarChar);
        dp.AddParam("@fs_no_telp", dto.fs_no_telp, SqlDbType.VarChar);
        dp.AddParam("@fb_default", dto.fb_default, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PasienTelpDto dto, string jnsTelp)
    {
        const string sql = """
            UPDATE 
                tc_mr_telp
            SET 
               fs_no_telp = @fs_no_telp,
               fb_default = @fb_default
            WHERE
               fs_mr = @fs_mr
               AND fs_kd_jenis_telp = @fs_kd_jenis_telp
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", dto.fs_mr, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_jenis_telp", jnsTelp, SqlDbType.VarChar);
        dp.AddParam("@fs_no_telp", dto.fs_no_telp, SqlDbType.VarChar);
        dp.AddParam("@fb_default", dto.fb_default, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    public void Delete(IPasienKey key)
    {
        const string sql = """
            DELETE FROM 
                tc_mr_id
            WHERE
               fs_mr = @fs_mr
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", key.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PasienTelpDto> ListData(IPasienKey filter)
    {
        const string sql = """
            SELECT 
                aa.fs_mr, aa.fs_kd_jenis_telp, aa.fs_no_telp, aa.fb_default
            FROM 
                tc_mr_telp aa
            WHERE 
                aa.fs_mr = @PasienId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PasienId", filter.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PasienTelpDto>(sql, dp);
    }
}
