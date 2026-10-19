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
    private const string SelectColumns = """
        RoomChargeId, KomponenId, KomponenName, KomponenCategory,
        Tarif, Diskon, Total, RekeningPendapatan
        """;

    private readonly DatabaseOptions _opt;

    public RoomChargeKomponenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
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
            INSERT INTO BILRG_RnaRoomChargeKomponen (
                RoomChargeId, KomponenId, KomponenName, KomponenCategory,
                Tarif, Diskon, Total, RekeningPendapatan)
            VALUES (
                @RoomChargeId, @KomponenId, @KomponenName, @KomponenCategory,
                @Tarif, @Diskon, @Total, @RekeningPendapatan)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, rows.Select(MapParams));
    }

    public void DeleteByRoomChargeId(string roomChargeId)
    {
        const string sql = """
            DELETE FROM BILRG_RnaRoomChargeKomponen
            WHERE RoomChargeId = @RoomChargeId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RoomChargeId", roomChargeId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<RoomChargeKomponenDto> ListDataByRoomChargeId(string roomChargeId)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_RnaRoomChargeKomponen
            WHERE RoomChargeId = @RoomChargeId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RoomChargeId", roomChargeId, SqlDbType.VarChar);

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
        dp.AddParam("@RoomChargeId", dto.RoomChargeId, SqlDbType.VarChar);
        dp.AddParam("@KomponenId", dto.KomponenId, SqlDbType.VarChar);
        dp.AddParam("@KomponenName", dto.KomponenName, SqlDbType.VarChar);
        dp.AddParam("@KomponenCategory", dto.KomponenCategory, SqlDbType.VarChar);
        dp.AddParam("@Tarif", dto.Tarif, SqlDbType.Decimal);
        dp.AddParam("@Diskon", dto.Diskon, SqlDbType.Decimal);
        dp.AddParam("@Total", dto.Total, SqlDbType.Decimal);
        dp.AddParam("@RekeningPendapatan", dto.RekeningPendapatan, SqlDbType.VarChar);
        return dp;
    }
}
