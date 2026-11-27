using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.RadContext.RadJadwalFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.RadContext.RadJadwalFeature;

public class RadEquipmentDal : IRadEquipmentDal
{
    private const string SelectColumns = """
        EquipmentId, EquipmentName, Modality, RoomId, RoomName, IsActive
        """;

    private readonly DatabaseOptions _opt;

    public RadEquipmentDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public RadEquipmentDto? GetData(string equipmentId)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_RadEquipment
            WHERE EquipmentId = @EquipmentId
            """;

        var dp = new DynamicParameters();
        dp.Add("@EquipmentId", equipmentId ?? string.Empty, DbType.AnsiString);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<RadEquipmentDto>(sql, dp);
    }

    public IEnumerable<RadEquipmentDto> ListByModality(string modality, bool activeOnly = true)
    {
        if (string.IsNullOrWhiteSpace(modality))
            return activeOnly ? ListActive() : ListAll();

        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_RadEquipment
            WHERE Modality IN @Modalities
            """;

        var dp = new DynamicParameters();
        dp.Add("@Modalities", ExpandModalityAliases(modality));

        if (activeOnly)
            sql += " AND IsActive = 1";

        sql += " ORDER BY EquipmentId ASC";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<RadEquipmentDto>(sql, dp) ?? Enumerable.Empty<RadEquipmentDto>();
    }

    public IEnumerable<RadEquipmentDto> ListActive()
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_RadEquipment
            WHERE IsActive = 1
            ORDER BY Modality ASC, EquipmentId ASC
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<RadEquipmentDto>(sql) ?? Enumerable.Empty<RadEquipmentDto>();
    }

    public IEnumerable<RadEquipmentDto> ListAll()
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_RadEquipment
            ORDER BY Modality ASC, EquipmentId ASC
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<RadEquipmentDto>(sql) ?? Enumerable.Empty<RadEquipmentDto>();
    }

    internal static string[] ExpandModalityAliases(string modality)
    {
        var normalized = modality.Trim().ToUpperInvariant();
        return normalized switch
        {
            "MR" or "MRI" => ["MR", "MRI"],
            "US" or "USG" => ["US", "USG"],
            _ => [modality.Trim()]
        };
    }
}
