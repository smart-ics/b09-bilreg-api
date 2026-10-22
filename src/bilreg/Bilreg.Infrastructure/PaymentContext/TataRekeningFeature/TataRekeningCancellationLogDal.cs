using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

public interface ITataRekeningCancellationLogDal : IInsert<TataRekeningCancellationLogDto>
{
}

public class TataRekeningCancellationLogDal : ITataRekeningCancellationLogDal
{
    private readonly DatabaseOptions _opt;

    public TataRekeningCancellationLogDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TataRekeningCancellationLogDto dto)
    {
        const string sql = """
            INSERT INTO ta_trs_billing_log (
                fs_kd_log, fs_kd_reg, fs_user_spv, fs_user_aksi,
                fd_tgl_batal, fs_alasan_batal, fs_status_sebelum, fs_status_setelah, fs_ip_address)
            VALUES (
                @fs_kd_log, @fs_kd_reg, @fs_user_spv, @fs_user_aksi,
                @fd_tgl_batal, @fs_alasan_batal, @fs_status_sebelum, @fs_status_setelah, @fs_ip_address)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_log", dto.fs_kd_log, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_reg", dto.fs_kd_reg, SqlDbType.VarChar);
        dp.AddParam("@fs_user_spv", dto.fs_user_spv, SqlDbType.VarChar);
        dp.AddParam("@fs_user_aksi", dto.fs_user_aksi, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_batal", dto.fd_tgl_batal, SqlDbType.DateTime);
        dp.AddParam("@fs_alasan_batal", dto.fs_alasan_batal, SqlDbType.NVarChar);
        dp.AddParam("@fs_status_sebelum", dto.fs_status_sebelum, SqlDbType.VarChar);
        dp.AddParam("@fs_status_setelah", dto.fs_status_setelah, SqlDbType.VarChar);
        dp.AddParam("@fs_ip_address", dto.fs_ip_address, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
}
