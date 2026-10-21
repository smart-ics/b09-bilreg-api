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

        var sanitized = fetched.Select(x => x with
        {
            ResepId = SafeTruncate(x.ResepId, 10),
            BrgId = SafeTruncate(x.BrgId, 13),
            BrgName = SafeTruncate(x.BrgName, 128),
            SatuanId = SafeTruncate(x.SatuanId, 3),
            RacikId = SafeTruncate(x.RacikId, 13),
            DosisTxt = SafeTruncate(x.DosisTxt, 10),
            Signa = SafeTruncate(x.Signa, 150),
            Etiket = SafeTruncate(x.Etiket, 225),
            Route = SafeTruncate(x.Route, 3),
            MealRelation = SafeTruncate(x.MealRelation, 70),
            UddTime = SafeTruncate(x.UddTime, 30)
        }).ToList();

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

        bcp.AddMap("Route", "fs_etiket_kd_pakai");
        bcp.AddMap("MealRelation", "fs_etiket_catatan");
        bcp.AddMap("UddTime", "fs_udd_waktu");
        bcp.AddMap("IsVoid", "fb_void");

        bcp.BatchSize = sanitized.Count;
        bcp.DestinationTableName = "dbo.ta_trs_kartu_periksa3";
        bcp.WriteToServer(sanitized.AsDataTable());
    }

    private static string SafeTruncate(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLen ? value : value[..maxLen];
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
                CAST(aa.fn_no_urut AS DECIMAL(18,2)) AS NoUrut,
                aa.fs_kd_barang AS BrgId,
                aa.fs_nm_barang AS BrgName,
                aa.fn_qty_barang AS Qty,
                aa.fs_kd_satuan AS SatuanId,
                CAST(aa.fn_iter_barang AS DECIMAL(18,2)) AS Iter,
                aa.fb_racik AS IsRacik,
                aa.fb_komponen AS IsKomponen,
                aa.fs_kd_racik AS RacikId,
                aa.fn_qty_racik AS Dosis,
                aa.fs_racik_qty AS DosisTxt,
                aa.fs_keterangan AS Signa,
                aa.fs_etiket_desc AS Etiket,
                CAST(aa.fn_etiket_qty AS DECIMAL(18,2)) AS Frequency,
                aa.fn_etiket_hari AS UnitDose,
                ISNULL(bb.fs_nm_satuan, '') AS SatuanName,
                ISNULL(aa.fs_etiket_kd_pakai, '') AS Route,
                ISNULL(aa.fs_etiket_catatan, '') AS MealRelation,
                ISNULL(aa.fs_udd_waktu, '') AS UddTime,
                aa.fb_void AS IsVoid
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
        return conn.Read<ResepBrgDto>(sql, dp) ?? [];
    }
}
