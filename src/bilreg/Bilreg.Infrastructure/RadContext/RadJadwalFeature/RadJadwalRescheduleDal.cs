using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.RadContext.RadJadwalFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.RadContext.RadJadwalFeature;

public interface IRadJadwalRescheduleDal : IInsert<RadJadwalRescheduleDto>
{
    IEnumerable<RadJadwalRescheduleDto> ListByJadwalRadId(string jadwalRadId);
}

public class RadJadwalRescheduleDal : IRadJadwalRescheduleDal
{
    private const string SelectColumns = """
        RescheduleId, JadwalRadId, RescheduleIndex,
        OldScheduleDate, OldStartTime, OldEndTime, OldEquipmentId, OldEquipmentName,
        Reason, RescheduledBy, RescheduledAt
        """;

    private readonly DatabaseOptions _opt;

    public RadJadwalRescheduleDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(RadJadwalRescheduleDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_RadJadwalReschedule (
                RescheduleId, JadwalRadId, RescheduleIndex,
                OldScheduleDate, OldStartTime, OldEndTime, OldEquipmentId, OldEquipmentName,
                Reason, RescheduledBy, RescheduledAt
            ) VALUES (
                @RescheduleId, @JadwalRadId, @RescheduleIndex,
                @OldScheduleDate, @OldStartTime, @OldEndTime, @OldEquipmentId, @OldEquipmentName,
                @Reason, @RescheduledBy, @RescheduledAt
            )
            """;

        var dp = new DynamicParameters();
        dp.Add("@RescheduleId", dto.RescheduleId ?? string.Empty, DbType.AnsiString);
        dp.Add("@JadwalRadId", dto.JadwalRadId ?? string.Empty, DbType.AnsiString);
        dp.Add("@RescheduleIndex", dto.RescheduleIndex, DbType.Int32);
        dp.Add("@OldScheduleDate", dto.OldScheduleDate.Date, DbType.Date);
        dp.Add("@OldStartTime", dto.OldStartTime, DbType.Time);
        dp.Add("@OldEndTime", dto.OldEndTime, DbType.Time);
        dp.Add("@OldEquipmentId", dto.OldEquipmentId ?? string.Empty, DbType.AnsiString);
        dp.Add("@OldEquipmentName", dto.OldEquipmentName ?? string.Empty, DbType.AnsiString);
        dp.Add("@Reason", dto.Reason ?? string.Empty, DbType.AnsiString);
        dp.Add("@RescheduledBy", dto.RescheduledBy ?? string.Empty, DbType.AnsiString);
        dp.Add("@RescheduledAt", dto.RescheduledAt, DbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<RadJadwalRescheduleDto> ListByJadwalRadId(string jadwalRadId)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_RadJadwalReschedule
            WHERE JadwalRadId = @JadwalRadId
            ORDER BY RescheduleIndex ASC
            """;

        var dp = new DynamicParameters();
        dp.Add("@JadwalRadId", jadwalRadId ?? string.Empty, DbType.AnsiString);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<RadJadwalRescheduleDto>(sql, dp) ?? Enumerable.Empty<RadJadwalRescheduleDto>();
    }
}
