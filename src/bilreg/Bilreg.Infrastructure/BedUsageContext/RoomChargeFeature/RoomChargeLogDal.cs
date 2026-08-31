using Bilreg.Domain.BedUsageContext.RoomChargeFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.BedUsageContext.RoomChargeFeature;

public interface IRoomChargeLogDal :
    IInsert<RoomChargeLogDto>
 { }
public class RoomChargeLogDal : IRoomChargeLogDal
{
    private readonly DatabaseOptions _opt;

    public RoomChargeLogDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public void Insert(RoomChargeLogDto dto)
    {
        const string sql = """
            INSERT INTO ta_trs_roomcharge_log (
                fs_kd_trs, fd_tgl_trs, fd_tgl_upd, fs_jam_upd,
                fs_kd_petugas, fs_kd_upd, fs_kd_reg,
                fs_kd_layanan, fs_kd_bed,
                fn_tarif, fn_qty, fb_status_void)
            VALUES (
                @fs_kd_trs, @fd_tgl_trs, @fd_tgl_upd, @fs_jam_upd,
                @fs_kd_petugas, @fs_kd_upd, @fs_kd_reg,
                @fs_kd_layanan, @fs_kd_bed,
                @fn_tarif, @fn_qty, @fb_status_void)
            """;

        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    private static DynamicParameters BuildParams(RoomChargeLogDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", dto.fs_kd_trs, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_trs", dto.fd_tgl_trs, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_upd", dto.fd_tgl_upd, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_upd", dto.fs_jam_upd, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas", dto.fs_kd_petugas, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_upd", dto.fs_kd_upd, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_reg", dto.fs_kd_reg, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_bed", dto.fs_kd_bed, SqlDbType.VarChar);
        dp.AddParam("@fn_tarif", dto.fn_tarif, SqlDbType.Decimal);
        dp.AddParam("@fn_qty", dto.fn_qty, SqlDbType.Decimal);
        dp.AddParam("@fb_status_void", dto.fb_status_void, SqlDbType.Bit);
        return dp;
    }

}
