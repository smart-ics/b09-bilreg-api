using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.SalesContext.ResepFeature;

public interface IResepBrgDal :
    IInsertBulk<ResepBrgDto>,
    IDelete<IResepKey>,
    IListData<ResepBrgDto, IResepKey>
{
}

public class ResepBrgDal : IResepBrgDal
{
    private readonly DatabaseOptions _opt;

    public ResepBrgDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<ResepBrgDto> listModel)
    {
        var fetched = listModel.ToList();
        if (fetched.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("ResepId", "fs_kd_trs");
        bcp.AddMap("NoUrut", "fn_no_urut");
        bcp.AddMap("BrgId", "fs_kd_barang");
        bcp.AddMap("BrgName", "fs_nm_barang");

        bcp.AddMap("Qty", "fn_qty_barang");
        bcp.AddMap("SatuanId", "fs_kd_satuan");
        bcp.AddMap("Iter", "fn_iter_barang");

        bcp.AddMap("IsRacik", "fb_racik");
        bcp.AddMap("IsKomponen", "fb_komponen");
        bcp.AddMap("RacikId", "fs_kd_racik");
        bcp.AddMap("Dosis", "fn_qty_racik");
        bcp.AddMap("DosisTxt", "fs_racik_qty");

        bcp.AddMap("Signa", "fs_keterangan");
        bcp.AddMap("Etiket", "fs_etiket_desc");
        bcp.AddMap("Frequency", "fn_etiket_qty");
        bcp.AddMap("UnitDose", "fn_etiket_hari");

        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "dbo.ta_trs_kartu_periksa3";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IResepKey key)
    {
        const string sql = """
            DELETE FROM ta_trs_kartu_periksa3
            WHERE fs_kd_trs = @ResepId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ResepId", key.ResepId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<ResepBrgDto> ListData(IResepKey filter)
    {
        const string sql = """
            SELECT
                aa.fs_kd_trs AS ResepId,
                aa.fn_no_urut AS NoUrut,
                aa.fs_kd_barang AS BrgId,
                aa.fs_nm_barang AS BrgName,
                aa.fn_qty_barang AS Qty,
                aa.fs_kd_satuan AS SatuanId,
                aa.fn_iter_barang AS Iter,
                aa.fb_racik AS IsRacik,
                aa.fb_komponen AS IsKomponen,
                aa.fs_kd_racik AS RacikId,
                aa.fn_qty_racik AS Dosis,
                aa.fs_racik_qty AS DosisTxt,
                aa.fs_keterangan AS Signa,
                aa.fs_etiket_desc AS Etiket,
                aa.fn_etiket_qty AS Frequency,
                aa.fn_etiket_hari AS UnitDose,
                ISNULL(bb.fs_nm_satuan, '') AS SatuanName
            FROM
                ta_trs_kartu_periksa3 aa
                LEFT JOIN tb_satuan bb ON aa.fs_kd_satuan = bb.fs_kd_satuan
            WHERE
                aa.fs_kd_trs = @ResepId
            ORDER BY
                aa.fn_no_urut
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ResepId", filter.ResepId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ResepBrgDto>(sql, dp);
    }
}
