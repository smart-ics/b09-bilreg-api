using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.RadContext.RadOrderFeature;

public interface IRadOrderTableDal :
    IRadOrderDal,
    IInsert<RadOrderDto>,
    IUpdate<RadOrderDto>,
    IGetData<RadOrderDto, IRadOrderKey>
{
    RadOrderDto? GetByEmrOrderId(string emrOrderId);
}

public class RadOrderDal : IRadOrderTableDal
{
    private static readonly DateTime VoidSentinel = new(3000, 1, 1);

    private const string SelectColumns = """
        aa.OrderId, aa.EmrOrderId, aa.OrderNo, aa.OrderSource, aa.Modality, aa.RadOrderStatus, aa.Urgency,
        aa.RegId, aa.PatientId, aa.PatientName, aa.BirthDate, aa.Gender, aa.AgeAtOrder,
        aa.DokterPengorderId, aa.DokterPengorderName, aa.LayananPengorderId, aa.LayananPengorderName,
        aa.ClinicalNote,
        aa.CancelledReason, aa.CancelledDate, aa.CancelledUserId,
        aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
        """;

    private readonly DatabaseOptions _opt;

    public RadOrderDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public RadOrderDto? GetData(IRadOrderKey key)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_RadOrder aa
            WHERE aa.OrderId = @OrderId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", key.OrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<RadOrderDto>(sql, dp);
    }

    public RadOrderDto? GetByEmrOrderId(string emrOrderId)
    {
        var sql = $"""
            SELECT TOP 1 {SelectColumns}
            FROM BILRG_RadOrder aa
            WHERE aa.EmrOrderId = @EmrOrderId AND aa.VodDate = @VodSentinel
            ORDER BY aa.CrtDate DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@EmrOrderId", emrOrderId, SqlDbType.VarChar);
        dp.AddParam("@VodSentinel", VoidSentinel, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<RadOrderDto>(sql, dp);
    }

    public IEnumerable<RadOrderDto> ListByRegId(string regId)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_RadOrder aa
            WHERE aa.RegId = @RegId AND aa.VodDate = @VodSentinel
            ORDER BY aa.CrtDate
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regId, SqlDbType.VarChar);
        dp.AddParam("@VodSentinel", VoidSentinel, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RadOrderDto>(sql, dp) ?? Enumerable.Empty<RadOrderDto>();
    }

    public bool HasRecentOrder(string patientId, string modality, IEnumerable<string> tarifIds, int windowHours)
    {
        if (string.IsNullOrWhiteSpace(patientId))
            return false;

        var tarifList = tarifIds?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? [];
        if (tarifList.Count == 0)
            return false;

        var since = DateTime.Now.AddHours(-Math.Abs(windowHours));
        const string sql = """
            SELECT TOP 1 1
            FROM BILRG_RadOrder aa
            INNER JOIN BILRG_RadOrderItem bb ON aa.OrderId = bb.OrderId
            WHERE aa.PatientId = @PatientId
              AND aa.Modality = @Modality
              AND aa.RadOrderStatus <> @CancelledStatus
              AND aa.VodDate = @VodSentinel
              AND aa.CrtDate >= @Since
              AND bb.TarifId IN @TarifIds
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var count = conn.ExecuteScalar<int?>(sql, new
        {
            PatientId = patientId,
            Modality = modality,
            CancelledStatus = (int)RadOrderStatusEnum.Cancelled,
            VodSentinel = VoidSentinel,
            Since = since,
            TarifIds = tarifList
        });

        return count.HasValue && count.Value > 0;
    }

    public IEnumerable<RadOrderWorklistView> GetWorklist(DateTime date, string modality, int? status = null)
    {
        var sql = """
            SELECT
                aa.OrderId,
                aa.OrderNo,
                aa.CrtDate,
                aa.Modality,
                aa.RadOrderStatus,
                aa.Urgency,
                aa.PatientId,
                aa.PatientName,
                aa.DokterPengorderName,
                aa.LayananPengorderName,
                ISNULL((SELECT COUNT(1) FROM BILRG_RadOrderItem bb WHERE bb.OrderId = aa.OrderId), 0) AS ItemCount,
                CASE WHEN aa.Urgency = 1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsCito
            FROM BILRG_RadOrder aa
            WHERE aa.VodDate = @VodSentinel
              AND aa.CrtDate >= @DateStart AND aa.CrtDate < @DateEnd
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@VodSentinel", VoidSentinel, SqlDbType.DateTime);
        dp.AddParam("@DateStart", date.Date, SqlDbType.DateTime);
        dp.AddParam("@DateEnd", date.Date.AddDays(1), SqlDbType.DateTime);

        if (!string.IsNullOrWhiteSpace(modality))
        {
            sql += " AND aa.Modality = @Modality";
            dp.AddParam("@Modality", modality, SqlDbType.VarChar);
        }

        if (status.HasValue)
        {
            sql += " AND aa.RadOrderStatus = @Status";
            dp.AddParam("@Status", status.Value, SqlDbType.Int);
        }

        sql += " ORDER BY aa.Urgency DESC, aa.CrtDate ASC";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<RadOrderWorklistView>(sql, dp) ?? Enumerable.Empty<RadOrderWorklistView>();
    }

    public void Insert(RadOrderDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_RadOrder (
                OrderId, EmrOrderId, OrderNo, OrderSource, Modality, RadOrderStatus, Urgency,
                RegId, PatientId, PatientName, BirthDate, Gender, AgeAtOrder,
                DokterPengorderId, DokterPengorderName, LayananPengorderId, LayananPengorderName,
                ClinicalNote,
                CancelledReason, CancelledDate, CancelledUserId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            ) VALUES (
                @OrderId, @EmrOrderId, @OrderNo, @OrderSource, @Modality, @RadOrderStatus, @Urgency,
                @RegId, @PatientId, @PatientName, @BirthDate, @Gender, @AgeAtOrder,
                @DokterPengorderId, @DokterPengorderName, @LayananPengorderId, @LayananPengorderName,
                @ClinicalNote,
                @CancelledReason, @CancelledDate, @CancelledUserId,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate
            )
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(RadOrderDto dto)
    {
        const string sql = """
            UPDATE BILRG_RadOrder
            SET EmrOrderId = @EmrOrderId,
                OrderNo = @OrderNo,
                OrderSource = @OrderSource,
                Modality = @Modality,
                RadOrderStatus = @RadOrderStatus,
                Urgency = @Urgency,
                RegId = @RegId,
                PatientId = @PatientId,
                PatientName = @PatientName,
                BirthDate = @BirthDate,
                Gender = @Gender,
                AgeAtOrder = @AgeAtOrder,
                DokterPengorderId = @DokterPengorderId,
                DokterPengorderName = @DokterPengorderName,
                LayananPengorderId = @LayananPengorderId,
                LayananPengorderName = @LayananPengorderName,
                ClinicalNote = @ClinicalNote,
                CancelledReason = @CancelledReason,
                CancelledDate = @CancelledDate,
                CancelledUserId = @CancelledUserId,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE OrderId = @OrderId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    private static DynamicParameters BuildParams(RadOrderDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", dto.OrderId, SqlDbType.VarChar);
        dp.AddParam("@EmrOrderId", dto.EmrOrderId, SqlDbType.VarChar);
        dp.AddParam("@OrderNo", dto.OrderNo, SqlDbType.VarChar);
        dp.AddParam("@OrderSource", dto.OrderSource, SqlDbType.Int);
        dp.AddParam("@Modality", dto.Modality, SqlDbType.VarChar);
        dp.AddParam("@RadOrderStatus", dto.RadOrderStatus, SqlDbType.Int);
        dp.AddParam("@Urgency", dto.Urgency, SqlDbType.Int);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PatientId", dto.PatientId, SqlDbType.VarChar);
        dp.AddParam("@PatientName", dto.PatientName, SqlDbType.VarChar);
        dp.AddParam("@BirthDate", dto.BirthDate, SqlDbType.DateTime);
        dp.AddParam("@Gender", dto.Gender, SqlDbType.VarChar);
        dp.AddParam("@AgeAtOrder", dto.AgeAtOrder, SqlDbType.Int);
        dp.AddParam("@DokterPengorderId", dto.DokterPengorderId, SqlDbType.VarChar);
        dp.AddParam("@DokterPengorderName", dto.DokterPengorderName, SqlDbType.VarChar);
        dp.AddParam("@LayananPengorderId", dto.LayananPengorderId, SqlDbType.VarChar);
        dp.AddParam("@LayananPengorderName", dto.LayananPengorderName, SqlDbType.VarChar);
        dp.AddParam("@ClinicalNote", dto.ClinicalNote, SqlDbType.VarChar);
        dp.AddParam("@CancelledReason", dto.CancelledReason, SqlDbType.VarChar);
        dp.AddParam("@CancelledDate", dto.CancelledDate, SqlDbType.DateTime);
        dp.AddParam("@CancelledUserId", dto.CancelledUserId, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
