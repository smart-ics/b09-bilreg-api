using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;

public interface ITrsBilling2Dal :
    IInsertBulk<TaTrsBilling2Dto>,
    IDelete<ITrsBillingKey>,
    IListData<TaTrsBilling2Dto, ITrsBillingKey>,
    IListData<TaTrsBilling2Dto, IRegKey>
{
}

public class TrsBilling2Dal : ITrsBilling2Dal
{
    private readonly DatabaseOptions _opt;
    public TrsBilling2Dal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(IEnumerable<TaTrsBilling2Dto> models)
    {
        const string sql = """
            INSERT INTO ta_trs_billing2 (
                fs_kd_trs, fn_no_urut, fs_kd_jenis_bayar, fn_trs_p, fn_trs_n,
                fs_kd_trs_bayar, fd_tgl_bayar, fs_jam_bayar, fs_kd_petugas_kasir,
                fs_kd_petugas_medis, fs_kd_detil_tarif, fs_kd_grup_rek,
                fs_kd_rek_ppdp, fs_kd_rek_pdpt, fs_kd_rek_disc,
                fs_kd_rek_pdpt_lain, fs_kd_rek_persediaan,
                fs_kd_rek_tax, fs_kd_rek_retur)
            VALUES (
                @fs_kd_trs, @fn_no_urut, @fs_kd_jenis_bayar, @fn_trs_p, @fn_trs_n,
                @fs_kd_trs_bayar, @fd_tgl_bayar, @fs_jam_bayar, @fs_kd_petugas_kasir,
                @fs_kd_petugas_medis, @fs_kd_detil_tarif, @fs_kd_grup_rek,
                @fs_kd_rek_ppdp, @fs_kd_rek_pdpt, @fs_kd_rek_disc,
                @fs_kd_rek_pdpt_lain, @fs_kd_rek_persediaan,
                @fs_kd_rek_tax, @fs_kd_rek_retur)
            """;
        
        var listBill2 = models.ToList();
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        
        var result = conn.Execute(sql, listBill2.Select(item => new
        {
            fs_kd_trs = item.fs_kd_trs,
            fn_no_urut = item.fn_no_urut, 
            fs_kd_jenis_bayar = item.fs_kd_jenis_bayar,
            fn_trs_p = item.fn_trs_p,
            fn_trs_n = item.fn_trs_n,
            fs_kd_trs_bayar = item.fs_kd_trs_bayar,
            fd_tgl_bayar = item.fd_tgl_bayar,
            fs_jam_bayar = item.fs_jam_bayar,
            fs_kd_petugas_kasir = item.fs_kd_petugas_kasir,
            fs_kd_petugas_medis = item.fs_kd_petugas_medis,
            fs_kd_detil_tarif = item.fs_kd_detil_tarif,
            fs_kd_grup_rek = item.fs_kd_grup_rek,
            fs_kd_rek_ppdp = item.fs_kd_rek_ppdp,
            fs_kd_rek_pdpt = item.fs_kd_rek_pdpt,
            fs_kd_rek_disc = item.fs_kd_rek_disc,
            fs_kd_rek_pdpt_lain = item.fs_kd_rek_pdpt_lain,
            fs_kd_rek_persediaan = item.fs_kd_rek_persediaan,
            fs_kd_rek_tax = item.fs_kd_rek_tax,
            fs_kd_rek_retur = item.fs_kd_rek_retur
        }));
    }
    public void Delete(ITrsBillingKey key)
    {
        const string sql = """
            DELETE FROM
                ta_trs_billing2
            WHERE
                fs_kd_trs = @fs_kd_trs
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.TrsBillingId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public IEnumerable<TaTrsBilling2Dto> ListData(ITrsBillingKey key)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.TrsBillingId, SqlDbType.VarChar);
        return ListBill2Rows("aa.fs_kd_trs = @fs_kd_trs", dp, includeBillingJoin: false);
    }

    public IEnumerable<TaTrsBilling2Dto> ListData(IRegKey key)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", key.RegId, SqlDbType.VarChar);
        return ListBill2Rows("bill.fs_kd_reg = @fs_kd_reg", dp, includeBillingJoin: true);
    }

    private IEnumerable<TaTrsBilling2Dto> ListBill2Rows(
        string whereClause,
        DynamicParameters dp,
        bool includeBillingJoin)
    {
        var billingJoin = includeBillingJoin
            ? "INNER JOIN ta_trs_billing bill ON aa.fs_kd_trs = bill.fs_kd_trs"
            : string.Empty;
        var sql = $"""
            SELECT
                aa.fs_kd_trs, aa.fn_no_urut,
                aa.fs_kd_jenis_bayar, aa.fn_trs_p, aa.fn_trs_n,

                aa.fs_kd_trs_bayar, aa.fd_tgl_bayar, aa.fs_jam_bayar,
                aa.fs_kd_petugas_kasir, aa.fs_kd_petugas_medis,
                aa.fs_kd_detil_tarif, aa.fs_kd_grup_rek,
                
                aa.fs_kd_rek_ppdp, aa.fs_kd_rek_pdpt, aa.fs_kd_rek_disc, 
                aa.fs_kd_rek_pdpt_lain, aa.fs_kd_rek_persediaan, aa.fs_kd_rek_tax, 
                aa.fs_kd_rek_retur,

                ISNULL(bb.fs_nm_detil_tarif, '') AS fs_nm_detil_tarif, 
                ISNULL(cc.fs_nm_grup_rek, '') AS fs_nm_grup_rek, 
                ISNULL(ee.fs_nm_peg, '') AS fs_nm_peg_kasir, 
                ISNULL(dd.fs_nm_peg, '') AS fs_nm_peg_medis 
            FROM
                ta_trs_billing2 aa
                {billingJoin}
                LEFT JOIN ta_detil_tarif bb ON aa.fs_kd_detil_tarif = bb.fs_kd_detil_tarif
                LEFT JOIN tb_grup_rek cc ON aa.fs_kd_grup_rek = cc.fs_kd_grup_rek
                LEFT JOIN td_peg dd ON aa.fs_kd_petugas_medis = dd.fs_kd_peg
                LEFT JOIN td_peg ee ON aa.fs_kd_petugas_kasir = ee.fs_kd_peg
            WHERE
                {whereClause}
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TaTrsBilling2Dto>(sql, dp);
    }
}