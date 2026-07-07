using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.ReservationFeature;

public interface IReservationDal :
    IInsert<ReservationDto>,
    IUpdate<ReservationDto>,
    IGetData<ReservationDto, IReservationKey>,
    IListData<ReservationDto, ReservationListFilter>
{
}

public class ReservationDal : IReservationDal
{
    private static readonly DateTime VoidSentinel = new(3000, 1, 1);

    private readonly DatabaseOptions _opt;

    public ReservationDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(ReservationDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_AdmReservation (
                ReservationId, ReservationStatus,
                PasienId, PasienName, TglLahir, Gender,
                OpnameRequestId, PlannedDate,
                KelasId, KelasName, BangsalId, BangsalName, RealizedRegId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @ReservationId, @ReservationStatus,
                @PasienId, @PasienName, @TglLahir, @Gender,
                @OpnameRequestId, @PlannedDate,
                @KelasId, @KelasName, @BangsalId, @BangsalName, @RealizedRegId,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public void Update(ReservationDto dto)
    {
        const string sql = """
            UPDATE BILRG_AdmReservation
            SET
                ReservationStatus = @ReservationStatus,
                PasienId = @PasienId,
                PasienName = @PasienName,
                TglLahir = @TglLahir,
                Gender = @Gender,
                OpnameRequestId = @OpnameRequestId,
                PlannedDate = @PlannedDate,
                KelasId = @KelasId,
                KelasName = @KelasName,
                BangsalId = @BangsalId,
                BangsalName = @BangsalName,
                RealizedRegId = @RealizedRegId,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE
                ReservationId = @ReservationId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public ReservationDto GetData(IReservationKey key)
    {
        const string sql = """
            SELECT
                ReservationId, ReservationStatus,
                PasienId, PasienName, TglLahir, Gender,
                OpnameRequestId, PlannedDate,
                KelasId, KelasName, BangsalId, BangsalName, RealizedRegId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM BILRG_AdmReservation
            WHERE ReservationId = @ReservationId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ReservationId", key.ReservationId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<ReservationDto>(sql, dp);
    }

    public IEnumerable<ReservationDto> ListData(ReservationListFilter filter)
    {
        const string sql = """
            SELECT
                aa.ReservationId, aa.ReservationStatus,
                aa.PasienId, aa.PasienName, aa.TglLahir, aa.Gender,
                aa.OpnameRequestId, aa.PlannedDate,
                aa.KelasId, aa.KelasName, aa.BangsalId, aa.BangsalName, aa.RealizedRegId,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_AdmReservation aa
            WHERE
                aa.VodDate = @VodDate
                AND (@Status IS NULL OR aa.ReservationStatus = @Status)
                AND (@PlannedFrom IS NULL OR aa.PlannedDate >= @PlannedFrom)
                AND (@PlannedTo IS NULL OR aa.PlannedDate < @PlannedToEnd)
            ORDER BY aa.PlannedDate ASC, aa.ReservationId ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);
        dp.AddParam("@Status", filter.Status.HasValue ? (int)filter.Status.Value : null, SqlDbType.Int);
        dp.AddParam("@PlannedFrom", filter.PlannedFrom, SqlDbType.DateTime);
        dp.AddParam("@PlannedToEnd",
            filter.PlannedTo?.Date.AddDays(1),
            SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ReservationDto>(sql, dp) ?? [];
    }

    private static DynamicParameters MapParams(ReservationDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@ReservationId", dto.ReservationId, SqlDbType.VarChar);
        dp.AddParam("@ReservationStatus", dto.ReservationStatus, SqlDbType.Int);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@TglLahir", dto.TglLahir, SqlDbType.DateTime);
        dp.AddParam("@Gender", dto.Gender, SqlDbType.VarChar);
        dp.AddParam("@OpnameRequestId", dto.OpnameRequestId, SqlDbType.VarChar);
        dp.AddParam("@PlannedDate", dto.PlannedDate, SqlDbType.DateTime);
        dp.AddParam("@KelasId", dto.KelasId, SqlDbType.VarChar);
        dp.AddParam("@KelasName", dto.KelasName, SqlDbType.VarChar);
        dp.AddParam("@BangsalId", dto.BangsalId, SqlDbType.VarChar);
        dp.AddParam("@BangsalName", dto.BangsalName, SqlDbType.VarChar);
        dp.AddParam("@RealizedRegId", dto.RealizedRegId, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
