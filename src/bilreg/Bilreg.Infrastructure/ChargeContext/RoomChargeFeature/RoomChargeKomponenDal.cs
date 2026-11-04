using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;

public class RoomChargeKomponenDal : IRoomChargeKomponenDal
{
    private readonly DatabaseOptions _opt;

    public RoomChargeKomponenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    private static string SafeTruncate(string? val, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(val)) return string.Empty;
        return val.Length > maxLen ? val[..maxLen] : val;
    }

    public void Insert(RoomChargeKomponenDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        Insert([dto]);
    }

    public void Insert(IEnumerable<RoomChargeKomponenDto> listModel)
    {
        var rows = listModel?.ToList() ?? [];
        if (rows.Count == 0) return;

        const string sql = """
            INSERT INTO ta_trs_roomcharge2 (
                fs_kd_trs, fs_kd_detil_tarif,
                fn_tarif, fn_diskon, fn_total,
                fs_kd_rek_pdpt, fs_kd_rek_diskon)
            VALUES (
                @fs_kd_trs, @fs_kd_detil_tarif,
                @fn_tarif, @fn_diskon, @fn_total,
                @fs_kd_rek_pdpt, @fs_kd_rek_diskon)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, rows.Select(MapParams));
    }

    public void DeleteByRoomChargeId(string roomChargeId)
    {
        const string sql = """
            DELETE FROM ta_trs_roomcharge2
            WHERE fs_kd_trs = @fs_kd_trs
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", SafeTruncate(roomChargeId, 15), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<RoomChargeKomponenDto> ListDataByRoomChargeId(string roomChargeId)
    {
        const string sql = """
            SELECT 
                bb.fs_kd_trs AS RoomChargeId,
                bb.fs_kd_detil_tarif AS KomponenId,
                ISNULL(dt.fs_nm_detil_tarif, bb.fs_kd_detil_tarif) AS KomponenName,
                'Akomodasi' AS KomponenCategory,
                bb.fn_tarif AS Tarif,
                bb.fn_diskon AS Diskon,
                bb.fn_total AS Total,
                ISNULL(bb.fs_kd_rek_pdpt, '') AS RekeningPendapatan
            FROM ta_trs_roomcharge2 bb
            LEFT JOIN ta_detil_tarif dt ON bb.fs_kd_detil_tarif = dt.fs_kd_detil_tarif
            WHERE bb.fs_kd_trs = @fs_kd_trs
            ORDER BY bb.fs_kd_detil_tarif ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", SafeTruncate(roomChargeId, 15), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RoomChargeKomponenDto>(sql, dp) ?? [];
    }

    public IEnumerable<RoomChargeKomponenDto> ListData(IRoomChargeKey filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return ListDataByRoomChargeId(filter.RoomChargeId);
    }

    private static DynamicParameters MapParams(RoomChargeKomponenDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", SafeTruncate(dto.RoomChargeId, 15), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_detil_tarif", SafeTruncate(dto.KomponenId, 3), SqlDbType.VarChar);
        dp.AddParam("@fn_tarif", dto.Tarif, SqlDbType.Decimal);
        dp.AddParam("@fn_diskon", dto.Diskon, SqlDbType.Decimal);
        dp.AddParam("@fn_total", dto.Total, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_rek_pdpt", SafeTruncate(dto.RekeningPendapatan, 20), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rek_diskon", string.Empty, SqlDbType.VarChar);
        return dp;
    }
}
