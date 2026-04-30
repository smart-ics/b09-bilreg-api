using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;

public interface ITrsBilling2GenDal :
    IInsertBulk<TrsBilling2GenDto>,
    IDelete<ITrsBillingBayarKey>,
    IListData<TrsBilling2GenDto, IRegKey>
{
}
public class TrsBilling2GenDal : ITrsBilling2GenDal
{
    private readonly DatabaseOptions _opt;
    public TrsBilling2GenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<TrsBilling2GenDto> listModel)
    {
        const string sql = """
            INSERT INTO ta_trs_billing2 (
                fs_kd_trs, fn_no_urut, fs_kd_detil_tarif, fs_kd_grup_rek, 
                fs_kd_trs_bayar, fs_kd_jenis_bayar, fn_trs_p, fn_trs_n,
                fd_tgl_bayar, fs_jam_bayar, fs_kd_petugas_medis, fs_kd_petugas_kasir)
            VALUES (
                @fs_kd_trs, @fn_no_urut, @fs_kd_detil_tarif, @fs_kd_grup_rek, 
                @fs_kd_trs_bayar, @fs_kd_jenis_bayar, @fn_trs_p, @fn_trs_n,
                @fd_tgl_bayar, @fs_jam_bayar, @fs_kd_petugas_medis, @fs_kd_petugas_kasir)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();

        var result = conn.Execute(sql, listModel.Select(item => new
        {
            fs_kd_trs = item.fs_kd_trs,
            fn_no_urut = item.fn_no_urut,
            fs_kd_detil_tarif = item.fs_kd_detil_tarif,
            fs_kd_grup_rek = item.fs_kd_grup_rek,
            fs_kd_trs_bayar = item.fs_kd_trs_bayar,
            fs_kd_jenis_bayar = item.fs_kd_jenis_bayar,
            fn_trs_p = item.fn_trs_p,
            fn_trs_n = item.fn_trs_n,
            fd_tgl_bayar = item.fd_tgl_bayar,
            fs_jam_bayar = item.fs_jam_bayar,
            fs_kd_petugas_medis = item.fs_kd_petugas_medis,
            fs_kd_petugas_kasir = item.fs_kd_petugas_kasir
        }));
    }

    public void Delete(ITrsBillingBayarKey key)
    {
        const string sql = """
            DELETE FROM
                ta_trs_billing2
            WHERE
                fs_kd_trs_bayar = @fs_kd_trs_bayar
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs_bayar", key.TrsBayarId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<TrsBilling2GenDto> ListData(IRegKey filter)
    {
        const string sql = """
            SELECT
                aa.fs_kd_trs, aa.fn_no_urut, aa.fs_kd_detil_tarif, aa.fs_kd_grup_rek, 
                aa.fs_kd_trs_bayar, aa.fs_kd_jenis_bayar, aa.fn_trs_p, aa.fn_trs_n,
                aa.fd_tgl_bayar, aa.fs_jam_bayar, aa.fs_kd_petugas_medis, aa.fs_kd_petugas_kasir
            FROM
                ta_trs_billing2 aa
                INNER JOIN ta_trs_billing bb on aa.fs_kd_trs = bb.fs_kd_trs
            WHERE
                bb.fs_kd_reg = @fs_kd_reg
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", filter.RegId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TrsBilling2GenDto>(sql, dp);
    }
}
