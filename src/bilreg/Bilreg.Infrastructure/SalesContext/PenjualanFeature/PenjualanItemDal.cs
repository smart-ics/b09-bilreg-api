using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.SalesContext.PenjualanFeature;

public interface IPenjualanItemDal :
    IInsertBulk<PenjualanItemDto>,
    IDelete<IPenjualanKey>,
    IListData<PenjualanItemDto, IPenjualanKey>
{
}

public class PenjualanItemDal : IPenjualanItemDal
{
    private readonly DatabaseOptions _opt;

    public PenjualanItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<PenjualanItemDto> listModel)
    {
        var fetched = listModel.ToList();
        if (fetched.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("PenjualanId", "fs_kd_trs");
        bcp.AddMap("PenjualanItemId", "fs_kd_trs2");
        bcp.AddMap("NoUrut", "fn_no_urut");
        bcp.AddMap("IsVoided", "fb_void");
        bcp.AddMap("BrgId", "fs_kd_barang");
        bcp.AddMap("BrgName", "fs_nm_barang");
        bcp.AddMap("TipeBarangId", "fs_kd_tipe_barang");
        bcp.AddMap("IsRacik", "fb_racik");
        bcp.AddMap("IsKomponen", "fb_komponen");
        bcp.AddMap("RacikId", "fs_kd_racik");
        bcp.AddMap("Dosis", "fn_qty_racik");
        bcp.AddMap("DosisTxt", "fs_racik_qty");
        bcp.AddMap("Qty", "fn_qty_barang");
        bcp.AddMap("SatuanName", "fs_nm_satuan");
        bcp.AddMap("Harga", "fn_harga_satuan");
        bcp.AddMap("Diskon", "fn_diskon");
        bcp.AddMap("Embalase", "fn_biaya");
        bcp.AddMap("SubTotal", "fn_sub_total");
        bcp.AddMap("TaxProsen", "fn_tax_prosen");
        bcp.AddMap("Tax", "fn_tax_rupiah");
        bcp.AddMap("Fee", "fn_biaya_fee");
        bcp.AddMap("Total", "fn_total");
        bcp.AddMap("Etiket", "fs_etiket");
        bcp.AddMap("TipeJaminanId", "fs_kd_tipe_jaminan");
        bcp.AddMap("Bulat", "fn_bulat");
        bcp.AddMap("SatuanId", "fs_kd_satuan");
        bcp.AddMap("Frequency", "fn_etiket_qty");
        bcp.AddMap("UnitDose", "fn_etiket_hari");
        bcp.AddMap("Note", "fs_etiket_catatan");
        bcp.AddMap("NilaiKlaim", "fn_nilai_klaim");

        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "dbo.tb_trs_dobill_umum2";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IPenjualanKey key)
    {
        const string sql = """
            DELETE FROM tb_trs_dobill_umum2
            WHERE fs_kd_trs = @PenjualanId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PenjualanId", key.PenjualanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PenjualanItemDto> ListData(IPenjualanKey filter)
    {
        const string sql = """
            SELECT
                aa.fs_kd_trs AS PenjualanId,
                aa.fs_kd_trs2 AS PenjualanItemId,
                CAST(aa.fn_no_urut AS INT) AS NoUrut,
                aa.fb_void AS IsVoided,
                aa.fs_kd_barang AS BrgId,
                aa.fs_nm_barang AS BrgName,
                aa.fs_kd_tipe_barang AS TipeBarangId,
                aa.fb_racik AS IsRacik,
                aa.fb_komponen AS IsKomponen,
                aa.fs_kd_racik AS RacikId,
                aa.fn_qty_racik AS Dosis,
                aa.fs_racik_qty AS DosisTxt,
                aa.fn_qty_barang AS Qty,
                aa.fs_kd_satuan AS SatuanId,
                aa.fn_harga_satuan AS Harga,
                aa.fn_diskon AS Diskon,
                aa.fn_biaya AS Embalase,
                aa.fn_sub_total AS SubTotal,
                aa.fn_tax_prosen AS TaxProsen,
                aa.fn_tax_rupiah AS Tax,
                aa.fn_biaya_fee AS Fee,
                aa.fn_total AS Total,
                aa.fn_bulat AS Bulat,
                aa.fn_nilai_klaim AS NilaiKlaim,
                aa.fs_kd_tipe_jaminan AS TipeJaminanId,
                aa.fs_etiket AS Etiket,
                CAST(aa.fn_etiket_qty AS INT) AS Frequency,
                aa.fn_etiket_hari AS UnitDose,
                aa.fs_etiket_catatan AS Note,
                ISNULL(NULLIF(aa.fs_nm_satuan, ''), ISNULL(bb.fs_nm_satuan, '')) AS SatuanName
            FROM
                tb_trs_dobill_umum2 aa
                LEFT JOIN tb_satuan bb ON aa.fs_kd_satuan = bb.fs_kd_satuan
            WHERE
                aa.fs_kd_trs = @PenjualanId
            ORDER BY
                aa.fn_no_urut
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PenjualanId", filter.PenjualanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PenjualanItemDto>(sql, dp);
    }
}
