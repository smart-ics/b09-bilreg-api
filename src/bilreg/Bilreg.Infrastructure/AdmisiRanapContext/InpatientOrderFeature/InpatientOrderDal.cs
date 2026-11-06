using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.InpatientOrderFeature;

public interface IInpatientOrderDal :
    IInsert<InpatientOrderDto>,
    IUpdate<InpatientOrderDto>,
    IDelete<IInpatientOrderKey>,
    IGetData<InpatientOrderDto, IInpatientOrderKey>,
    IListData<InpatientOrderDto, InpatientOrderListFilter>
{
    IEnumerable<InpatientOrderDto> ListActive();
    InpatientOrderDto GetByExternalReference(string externalReferenceId, string externalSystemSource);
}

public class InpatientOrderDal : IInpatientOrderDal
{
    private static readonly DateTime VoidSentinel = new(3000, 1, 1);

    private const string SELECT_COLUMNS = """
        aa.InpatientOrderId, aa.OrderSource, aa.OrderStatus,
        aa.PasienId,
        ISNULL(bb.fs_nm_pasien, aa.PasienName) AS PasienName,
        ISNULL(TRY_CONVERT(DATETIME, bb.fd_tgl_lahir), aa.TglLahir) AS TglLahir,
        ISNULL(bb.fs_jns_kelamin, aa.Gender) AS Gender,
        aa.DokterId, aa.DokterName,
        aa.PlannedDate,
        aa.KelasId, aa.KelasName,
        aa.BangsalId, aa.BangsalName,
        aa.ClinicalNotes,
        aa.TipeJaminanId, aa.TipeJaminanName, aa.JaminanReffId,
        aa.FulfilledRegId,
        aa.ExternalReferenceId, aa.ExternalSystemSource,
        aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
        """;

    private readonly DatabaseOptions _opt;

    public InpatientOrderDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(InpatientOrderDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_AdmInpatientOrder (
                InpatientOrderId, OrderSource, OrderStatus,
                PasienId, PasienName, TglLahir, Gender,
                DokterId, DokterName,
                PlannedDate,
                KelasId, KelasName, BangsalId, BangsalName,
                ClinicalNotes,
                TipeJaminanId, TipeJaminanName, JaminanReffId,
                FulfilledRegId,
                ExternalReferenceId, ExternalSystemSource,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @InpatientOrderId, @OrderSource, @OrderStatus,
                @PasienId, @PasienName, @TglLahir, @Gender,
                @DokterId, @DokterName,
                @PlannedDate,
                @KelasId, @KelasName, @BangsalId, @BangsalName,
                @ClinicalNotes,
                @TipeJaminanId, @TipeJaminanName, @JaminanReffId,
                @FulfilledRegId,
                @ExternalReferenceId, @ExternalSystemSource,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public void Update(InpatientOrderDto dto)
    {
        const string sql = """
            UPDATE BILRG_AdmInpatientOrder
            SET
                OrderSource = @OrderSource,
                OrderStatus = @OrderStatus,
                PasienId = @PasienId,
                PasienName = @PasienName,
                TglLahir = @TglLahir,
                Gender = @Gender,
                DokterId = @DokterId,
                DokterName = @DokterName,
                PlannedDate = @PlannedDate,
                KelasId = @KelasId,
                KelasName = @KelasName,
                BangsalId = @BangsalId,
                BangsalName = @BangsalName,
                ClinicalNotes = @ClinicalNotes,
                TipeJaminanId = @TipeJaminanId,
                TipeJaminanName = @TipeJaminanName,
                JaminanReffId = @JaminanReffId,
                FulfilledRegId = @FulfilledRegId,
                ExternalReferenceId = @ExternalReferenceId,
                ExternalSystemSource = @ExternalSystemSource,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE
                InpatientOrderId = @InpatientOrderId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public void Delete(IInpatientOrderKey key)
    {
        const string sql = """
            DELETE FROM BILRG_AdmInpatientOrder
            WHERE InpatientOrderId = @InpatientOrderId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@InpatientOrderId", key.InpatientOrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public InpatientOrderDto GetData(IInpatientOrderKey key)
    {
        const string sql = $"""
            SELECT
                {SELECT_COLUMNS}
            FROM BILRG_AdmInpatientOrder aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE aa.InpatientOrderId = @InpatientOrderId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@InpatientOrderId", key.InpatientOrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<InpatientOrderDto>(sql, dp);
    }

    public IEnumerable<InpatientOrderDto> ListData(InpatientOrderListFilter filter)
    {
        const string sql = $"""
            SELECT
                {SELECT_COLUMNS}
            FROM BILRG_AdmInpatientOrder aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE
                aa.VodDate = @VodDate
                AND (@Status IS NULL OR aa.OrderStatus = @Status)
                AND (@OrderSource IS NULL OR aa.OrderSource = @OrderSource)
                AND (@PlannedFrom IS NULL OR aa.PlannedDate >= @PlannedFrom)
                AND (@PlannedTo IS NULL OR aa.PlannedDate < @PlannedToEnd)
            ORDER BY aa.PlannedDate ASC, aa.CrtDate ASC, aa.InpatientOrderId ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);
        dp.AddParam("@Status", filter.Status.HasValue ? (int)filter.Status.Value : null, SqlDbType.Int);
        dp.AddParam("@OrderSource", filter.OrderSource.HasValue ? (int)filter.OrderSource.Value : null, SqlDbType.Int);
        dp.AddParam("@PlannedFrom", filter.PlannedFrom, SqlDbType.DateTime);
        dp.AddParam("@PlannedTo", filter.PlannedTo, SqlDbType.DateTime);
        dp.AddParam("@PlannedToEnd", filter.PlannedTo?.Date.AddDays(1), SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<InpatientOrderDto>(sql, dp) ?? [];
    }

    public IEnumerable<InpatientOrderDto> ListActive()
    {
        const string sql = $"""
            SELECT
                {SELECT_COLUMNS}
            FROM BILRG_AdmInpatientOrder aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE
                aa.VodDate = @VodDate
                AND aa.OrderStatus IN (0, 1)
            ORDER BY aa.PlannedDate ASC, aa.CrtDate ASC, aa.InpatientOrderId ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<InpatientOrderDto>(sql, dp) ?? [];
    }

    public InpatientOrderDto GetByExternalReference(string externalReferenceId, string externalSystemSource)
    {
        const string sql = $"""
            SELECT
                {SELECT_COLUMNS}
            FROM BILRG_AdmInpatientOrder aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE aa.ExternalReferenceId = @ExternalReferenceId
              AND (@ExternalSystemSource = '' OR aa.ExternalSystemSource = @ExternalSystemSource)
              AND aa.VodDate = @VodDate
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ExternalReferenceId", externalReferenceId, SqlDbType.VarChar);
        dp.AddParam("@ExternalSystemSource", externalSystemSource ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<InpatientOrderDto>(sql, dp);
    }

    private static DynamicParameters MapParams(InpatientOrderDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@InpatientOrderId", dto.InpatientOrderId, SqlDbType.VarChar);
        dp.AddParam("@OrderSource", dto.OrderSource, SqlDbType.Int);
        dp.AddParam("@OrderStatus", dto.OrderStatus, SqlDbType.Int);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@TglLahir", dto.TglLahir, SqlDbType.DateTime);
        dp.AddParam("@Gender", dto.Gender, SqlDbType.VarChar);
        dp.AddParam("@DokterId", dto.DokterId, SqlDbType.VarChar);
        dp.AddParam("@DokterName", dto.DokterName, SqlDbType.VarChar);
        dp.AddParam("@PlannedDate", dto.PlannedDate, SqlDbType.DateTime);
        dp.AddParam("@KelasId", dto.KelasId, SqlDbType.VarChar);
        dp.AddParam("@KelasName", dto.KelasName, SqlDbType.VarChar);
        dp.AddParam("@BangsalId", dto.BangsalId, SqlDbType.VarChar);
        dp.AddParam("@BangsalName", dto.BangsalName, SqlDbType.VarChar);
        dp.AddParam("@ClinicalNotes", dto.ClinicalNotes, SqlDbType.VarChar);
        dp.AddParam("@TipeJaminanId", dto.TipeJaminanId, SqlDbType.VarChar);
        dp.AddParam("@TipeJaminanName", dto.TipeJaminanName, SqlDbType.VarChar);
        dp.AddParam("@JaminanReffId", dto.JaminanReffId, SqlDbType.VarChar);
        dp.AddParam("@FulfilledRegId", dto.FulfilledRegId, SqlDbType.VarChar);
        dp.AddParam("@ExternalReferenceId", dto.ExternalReferenceId, SqlDbType.VarChar);
        dp.AddParam("@ExternalSystemSource", dto.ExternalSystemSource, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
