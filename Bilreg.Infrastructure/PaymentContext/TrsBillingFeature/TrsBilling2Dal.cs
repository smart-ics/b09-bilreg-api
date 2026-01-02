using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;

public interface ITrsBilling2Dal :
    IInsertBulk<TaTrsBilling2Dto>,
    IDelete<ITrsBillingKey>,
    IListData<TaTrsBilling2Dto, ITrsBillingKey>
{
}

public class TrsBilling2Dal : ITrsBilling2Dal
{
    private readonly DatabaseOptions _opt;
    public TrsBilling2Dal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(IEnumerable<TaTrsBilling2Dto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        conn.Open();
        bcp.AddMap("fs_kd_trs", "fs_kd_trs");
        bcp.AddMap("fn_no_urut", "fn_no_urut");
        bcp.AddMap("fs_kd_detil_tarif", "fs_kd_detil_tarif");
        bcp.AddMap("fs_kd_grup_rek", "fs_kd_grup_rek");
        bcp.AddMap("fs_kd_trs_bayar", "fs_kd_trs_bayar");
        bcp.AddMap("fs_kd_jenis_bayar", "fs_kd_jenis_bayar");
        bcp.AddMap("fn_trs_p", "fn_trs_p");
        bcp.AddMap("fn_trs_n", "fn_trs_n");
        bcp.AddMap("fs_kd_petugas_medis", "fs_kd_petugas_medis");
        bcp.AddMap("fs_kd_petugas_kasir", "fs_kd_petugas_kasir");
        bcp.AddMap("fd_tgl_bayar", "fd_tgl_bayar");
        bcp.AddMap("fs_jam_bayar", "fs_jam_bayar");
        bcp.AddMap("fs_kd_rek_ppdp", "fs_kd_rek_ppdp");
        bcp.AddMap("fs_kd_rek_pdpt", "fs_kd_rek_pdpt");
        bcp.AddMap("fs_kd_rek_pdpt_lain", "fs_kd_rek_pdpt_lain");
        bcp.AddMap("fs_kd_rek_disc", "fs_kd_rek_disc");
        bcp.AddMap("fs_kd_rek_persediaan", "fs_kd_rek_persediaan");
        bcp.AddMap("fs_kd_rek_tax", "fs_kd_rek_tax");
        bcp.AddMap("fs_kd_rek_retur", "fs_kd_rek_retur");
        
        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "ta_trs_billing2";
        bcp.WriteToServer(fetched.AsDataTable());
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
        const string sql = """
            SELECT
                aa.fs_kd_trs,
                aa.fn_no_urut,
                aa.fs_kd_detil_tarif,
                aa.fs_kd_grup_rek,
                aa.fs_kd_trs_bayar,
                aa.fs_kd_jenis_bayar,
                aa.fn_trs_p,
                aa.fn_trs_n,
                aa.fs_kd_petugas_medis,
                aa.fs_kd_petugas_kasir,
                aa.fd_tgl_bayar,
                aa.fs_jam_bayar,
                aa.fs_kd_rek_ppdp,
                aa.fs_kd_rek_pdpt,
                aa.fs_kd_rek_pdpt_lain,
                aa.fs_kd_rek_disc,
                aa.fs_kd_rek_persediaan,
                aa.fs_kd_rek_tax,
                aa.fs_kd_rek_retur,
                ISNULL(bb.fs_nm_detil_tarif, '') AS fs_nm_detil_tarif, 
                ISNULL(cc.fs_nm_grup_rek, '') AS fs_nm_grup_rek, 
                ISNULL(dd.fs_nm_peg, '') AS fs_nm_peg_medis, 
                ISNULL(ee.fs_nm_peg, '') AS fs_nm_peg_kasir 
            FROM
                ta_trs_billing2 aa
                left join ta_detil_tarif bb on aa.fs_kd_detil_tarif = bb.fs_kd_detil_tarif
                left join tb_grup_rek cc on aa.fs_kd_grup_rek = cc.fs_kd_grup_rek
                left join td_peg dd on aa.fs_kd_petugas_medis = dd.fs_kd_peg
                left join td_peg ee on aa.fs_kd_petugas_kasir = ee.fs_kd_peg
            WHERE
                fs_kd_trs = @fs_kd_trs
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.TrsBillingId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TaTrsBilling2Dto>(sql, dp);
    }
}