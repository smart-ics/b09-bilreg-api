using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.RadContext.RadJadwalFeature;
using Bilreg.Domain.RadContext.RadJadwalFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.RadContext.RadJadwalFeature;

public class RadJadwalDal : IRadJadwalDal
{
    private const string SelectColumns = """
        aa.JadwalRadId, aa.ScheduleNo, aa.OrderId, aa.OrderNo, aa.RegId,
        aa.PasienId, aa.PasienName, aa.BirthDate, aa.Gender, aa.Phone,
        aa.Modality, aa.EquipmentId, aa.EquipmentName, aa.RoomId, aa.RoomName,
        aa.DoctorRadiologyId, aa.DoctorRadiologyName,
        aa.ScheduleDate, aa.StartTime, aa.EndTime, aa.DurationMinutes, aa.TurnaroundBufferMinutes,
        aa.PreparationNotes, aa.LabRequirements, aa.SafetyChecklistJson,
        aa.IsArrived, aa.ArrivedAt, aa.IsCleared, aa.ClearanceNotes, aa.ClearedUserId, aa.ClearedAt,
        aa.Status, aa.RescheduleCount, aa.CancellationCategory, aa.CancellationReason,
        aa.CreatedUserId, aa.CreatedDate, aa.ModifiedUserId, aa.ModifiedDate
        """;

    private const string CalendarSlotColumns = """
        aa.JadwalRadId, aa.ScheduleNo, aa.OrderId, aa.OrderNo, aa.RegId,
        aa.PasienId, aa.PasienName, aa.Modality,
        aa.EquipmentId, aa.EquipmentName, aa.RoomId, aa.RoomName,
        aa.DoctorRadiologyId, aa.DoctorRadiologyName,
        aa.ScheduleDate, aa.StartTime, aa.EndTime, aa.DurationMinutes, aa.TurnaroundBufferMinutes,
        aa.Status, aa.IsArrived, aa.IsCleared, aa.RescheduleCount
        """;

    private readonly DatabaseOptions _opt;
    private readonly RadJadwalRescheduleDal _rescheduleDal;

    public RadJadwalDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
        _rescheduleDal = new RadJadwalRescheduleDal(opt);
    }

    public void Insert(RadJadwalDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_RadJadwal (
                JadwalRadId, ScheduleNo, OrderId, OrderNo, RegId,
                PasienId, PasienName, BirthDate, Gender, Phone,
                Modality, EquipmentId, EquipmentName, RoomId, RoomName,
                DoctorRadiologyId, DoctorRadiologyName,
                ScheduleDate, StartTime, EndTime, DurationMinutes, TurnaroundBufferMinutes,
                PreparationNotes, LabRequirements, SafetyChecklistJson,
                IsArrived, ArrivedAt, IsCleared, ClearanceNotes, ClearedUserId, ClearedAt,
                Status, RescheduleCount, CancellationCategory, CancellationReason,
                CreatedUserId, CreatedDate, ModifiedUserId, ModifiedDate
            ) VALUES (
                @JadwalRadId, @ScheduleNo, @OrderId, @OrderNo, @RegId,
                @PasienId, @PasienName, @BirthDate, @Gender, @Phone,
                @Modality, @EquipmentId, @EquipmentName, @RoomId, @RoomName,
                @DoctorRadiologyId, @DoctorRadiologyName,
                @ScheduleDate, @StartTime, @EndTime, @DurationMinutes, @TurnaroundBufferMinutes,
                @PreparationNotes, @LabRequirements, @SafetyChecklistJson,
                @IsArrived, @ArrivedAt, @IsCleared, @ClearanceNotes, @ClearedUserId, @ClearedAt,
                @Status, @RescheduleCount, @CancellationCategory, @CancellationReason,
                @CreatedUserId, @CreatedDate, @ModifiedUserId, @ModifiedDate
            )
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(RadJadwalDto dto)
    {
        const string sql = """
            UPDATE BILRG_RadJadwal
            SET ScheduleNo = @ScheduleNo,
                OrderId = @OrderId,
                OrderNo = @OrderNo,
                RegId = @RegId,
                PasienId = @PasienId,
                PasienName = @PasienName,
                BirthDate = @BirthDate,
                Gender = @Gender,
                Phone = @Phone,
                Modality = @Modality,
                EquipmentId = @EquipmentId,
                EquipmentName = @EquipmentName,
                RoomId = @RoomId,
                RoomName = @RoomName,
                DoctorRadiologyId = @DoctorRadiologyId,
                DoctorRadiologyName = @DoctorRadiologyName,
                ScheduleDate = @ScheduleDate,
                StartTime = @StartTime,
                EndTime = @EndTime,
                DurationMinutes = @DurationMinutes,
                TurnaroundBufferMinutes = @TurnaroundBufferMinutes,
                PreparationNotes = @PreparationNotes,
                LabRequirements = @LabRequirements,
                SafetyChecklistJson = @SafetyChecklistJson,
                IsArrived = @IsArrived,
                ArrivedAt = @ArrivedAt,
                IsCleared = @IsCleared,
                ClearanceNotes = @ClearanceNotes,
                ClearedUserId = @ClearedUserId,
                ClearedAt = @ClearedAt,
                Status = @Status,
                RescheduleCount = @RescheduleCount,
                CancellationCategory = @CancellationCategory,
                CancellationReason = @CancellationReason,
                ModifiedUserId = @ModifiedUserId,
                ModifiedDate = @ModifiedDate
            WHERE JadwalRadId = @JadwalRadId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public RadJadwalDto? GetData(IRadJadwalKey key)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_RadJadwal aa
            WHERE aa.JadwalRadId = @JadwalRadId
            """;

        var dp = new DynamicParameters();
        dp.Add("@JadwalRadId", key.JadwalRadId ?? string.Empty, DbType.AnsiString);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<RadJadwalDto>(sql, dp);
    }

    public RadJadwalDto? GetActiveByOrderId(string orderId)
    {
        var sql = $"""
            SELECT TOP 1 {SelectColumns}
            FROM BILRG_RadJadwal aa
            WHERE aa.OrderId = @OrderId
              AND aa.Status = @ScheduledStatus
            ORDER BY aa.CreatedDate DESC
            """;

        var dp = new DynamicParameters();
        dp.Add("@OrderId", orderId ?? string.Empty, DbType.AnsiString);
        dp.Add("@ScheduledStatus", (int)RadJadwalStatusEnum.Scheduled, DbType.Int32);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<RadJadwalDto>(sql, dp);
    }

    public IEnumerable<RadJadwalDto> ListByOrderId(string orderId)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_RadJadwal aa
            WHERE aa.OrderId = @OrderId
            ORDER BY aa.ScheduleDate DESC, aa.StartTime DESC
            """;

        var dp = new DynamicParameters();
        dp.Add("@OrderId", orderId ?? string.Empty, DbType.AnsiString);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<RadJadwalDto>(sql, dp) ?? Enumerable.Empty<RadJadwalDto>();
    }

    public IEnumerable<RadJadwalDto> ListByDateRange(
        DateTime startDate,
        DateTime endDate,
        string? modality = null,
        int? status = null,
        string? equipmentId = null)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_RadJadwal aa
            WHERE aa.ScheduleDate >= @StartDate
              AND aa.ScheduleDate <= @EndDate
            """;

        var dp = new DynamicParameters();
        dp.Add("@StartDate", startDate.Date, DbType.Date);
        dp.Add("@EndDate", endDate.Date, DbType.Date);

        if (!string.IsNullOrWhiteSpace(modality))
        {
            sql += " AND aa.Modality IN @Modalities";
            dp.Add("@Modalities", RadEquipmentDal.ExpandModalityAliases(modality));
        }

        if (status.HasValue)
        {
            sql += " AND aa.Status = @Status";
            dp.Add("@Status", status.Value, DbType.Int32);
        }

        if (!string.IsNullOrWhiteSpace(equipmentId))
        {
            sql += " AND aa.EquipmentId = @EquipmentId";
            dp.Add("@EquipmentId", equipmentId, DbType.AnsiString);
        }

        sql += " ORDER BY aa.ScheduleDate ASC, aa.StartTime ASC";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<RadJadwalDto>(sql, dp) ?? Enumerable.Empty<RadJadwalDto>();
    }

    public IEnumerable<RadJadwalCalendarSlotDto> ListCalendarSlots(
        DateTime scheduleDate,
        string? modality = null,
        string? equipmentId = null)
    {
        var sql = $"""
            SELECT {CalendarSlotColumns}
            FROM BILRG_RadJadwal aa
            WHERE aa.ScheduleDate = @ScheduleDate
              AND aa.Status <> @CancelledStatus
            """;

        var dp = new DynamicParameters();
        dp.Add("@ScheduleDate", scheduleDate.Date, DbType.Date);
        dp.Add("@CancelledStatus", (int)RadJadwalStatusEnum.Cancelled, DbType.Int32);

        if (!string.IsNullOrWhiteSpace(modality))
        {
            sql += " AND aa.Modality IN @Modalities";
            dp.Add("@Modalities", RadEquipmentDal.ExpandModalityAliases(modality));
        }

        if (!string.IsNullOrWhiteSpace(equipmentId))
        {
            sql += " AND aa.EquipmentId = @EquipmentId";
            dp.Add("@EquipmentId", equipmentId, DbType.AnsiString);
        }

        sql += " ORDER BY aa.EquipmentId ASC, aa.StartTime ASC";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<RadJadwalCalendarSlotDto>(sql, dp) ?? Enumerable.Empty<RadJadwalCalendarSlotDto>();
    }

    public bool IsSlotOverlapping(
        string equipmentId,
        DateTime scheduleDate,
        TimeSpan startTime,
        TimeSpan endTime,
        string? excludeJadwalId = null)
    {
        var sql = """
            SELECT TOP 1 1
            FROM BILRG_RadJadwal WITH (UPDLOCK, HOLDLOCK)
            WHERE EquipmentId = @EquipmentId
              AND ScheduleDate = @ScheduleDate
              AND Status = @ScheduledStatus
              AND StartTime < @EndTime
              AND EndTime > @StartTime
            """;

        var dp = new DynamicParameters();
        dp.Add("@EquipmentId", equipmentId ?? string.Empty, DbType.AnsiString);
        dp.Add("@ScheduleDate", scheduleDate.Date, DbType.Date);
        dp.Add("@ScheduledStatus", (int)RadJadwalStatusEnum.Scheduled, DbType.Int32);
        dp.Add("@StartTime", startTime, DbType.Time);
        dp.Add("@EndTime", endTime, DbType.Time);

        if (!string.IsNullOrWhiteSpace(excludeJadwalId))
        {
            sql += " AND JadwalRadId <> @ExcludeJadwalId";
            dp.Add("@ExcludeJadwalId", excludeJadwalId, DbType.AnsiString);
        }

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var count = conn.ExecuteScalar<int?>(sql, dp);
        return count.HasValue && count.Value > 0;
    }

    public void InsertReschedule(RadJadwalRescheduleDto dto) =>
        _rescheduleDal.Insert(dto);

    public IEnumerable<RadJadwalRescheduleDto> ListRescheduleHistory(string jadwalRadId) =>
        _rescheduleDal.ListByJadwalRadId(jadwalRadId);

    private static DynamicParameters BuildParams(RadJadwalDto dto)
    {
        var dp = new DynamicParameters();
        dp.Add("@JadwalRadId", dto.JadwalRadId ?? string.Empty, DbType.AnsiString);
        dp.Add("@ScheduleNo", dto.ScheduleNo ?? string.Empty, DbType.AnsiString);
        dp.Add("@OrderId", dto.OrderId ?? string.Empty, DbType.AnsiString);
        dp.Add("@OrderNo", dto.OrderNo ?? string.Empty, DbType.AnsiString);
        dp.Add("@RegId", dto.RegId ?? string.Empty, DbType.AnsiString);
        dp.Add("@PasienId", dto.PasienId ?? string.Empty, DbType.AnsiString);
        dp.Add("@PasienName", dto.PasienName ?? string.Empty, DbType.AnsiString);
        dp.Add("@BirthDate", dto.BirthDate, DbType.DateTime);
        dp.Add("@Gender", dto.Gender ?? string.Empty, DbType.AnsiString);
        dp.Add("@Phone", dto.Phone ?? string.Empty, DbType.AnsiString);
        dp.Add("@Modality", dto.Modality ?? string.Empty, DbType.AnsiString);
        dp.Add("@EquipmentId", dto.EquipmentId ?? string.Empty, DbType.AnsiString);
        dp.Add("@EquipmentName", dto.EquipmentName ?? string.Empty, DbType.AnsiString);
        dp.Add("@RoomId", dto.RoomId ?? string.Empty, DbType.AnsiString);
        dp.Add("@RoomName", dto.RoomName ?? string.Empty, DbType.AnsiString);
        dp.Add("@DoctorRadiologyId", dto.DoctorRadiologyId ?? string.Empty, DbType.AnsiString);
        dp.Add("@DoctorRadiologyName", dto.DoctorRadiologyName ?? string.Empty, DbType.AnsiString);
        dp.Add("@ScheduleDate", dto.ScheduleDate.Date, DbType.Date);
        dp.Add("@StartTime", dto.StartTime, DbType.Time);
        dp.Add("@EndTime", dto.EndTime, DbType.Time);
        dp.Add("@DurationMinutes", dto.DurationMinutes, DbType.Int32);
        dp.Add("@TurnaroundBufferMinutes", dto.TurnaroundBufferMinutes, DbType.Int32);
        dp.Add("@PreparationNotes", dto.PreparationNotes ?? string.Empty, DbType.AnsiString);
        dp.Add("@LabRequirements", dto.LabRequirements ?? string.Empty, DbType.AnsiString);
        dp.Add("@SafetyChecklistJson", dto.SafetyChecklistJson ?? string.Empty, DbType.AnsiString);
        dp.Add("@IsArrived", dto.IsArrived, DbType.Boolean);
        dp.Add("@ArrivedAt", dto.ArrivedAt, DbType.DateTime);
        dp.Add("@IsCleared", dto.IsCleared, DbType.Boolean);
        dp.Add("@ClearanceNotes", dto.ClearanceNotes ?? string.Empty, DbType.AnsiString);
        dp.Add("@ClearedUserId", dto.ClearedUserId ?? string.Empty, DbType.AnsiString);
        dp.Add("@ClearedAt", dto.ClearedAt, DbType.DateTime);
        dp.Add("@Status", dto.Status, DbType.Int32);
        dp.Add("@RescheduleCount", dto.RescheduleCount, DbType.Int32);
        dp.Add("@CancellationCategory", dto.CancellationCategory ?? string.Empty, DbType.AnsiString);
        dp.Add("@CancellationReason", dto.CancellationReason ?? string.Empty, DbType.AnsiString);
        dp.Add("@CreatedUserId", dto.CreatedUserId ?? string.Empty, DbType.AnsiString);
        dp.Add("@CreatedDate", dto.CreatedDate, DbType.DateTime);
        dp.Add("@ModifiedUserId", dto.ModifiedUserId ?? string.Empty, DbType.AnsiString);
        dp.Add("@ModifiedDate", dto.ModifiedDate, DbType.DateTime);
        return dp;
    }
}
