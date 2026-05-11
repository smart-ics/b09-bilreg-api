using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.IgdContext.IgdVisitFeature;

public interface IIgdVisitDal :
    IInsert<IgdVisitDto>,
    IUpdate<IgdVisitDto>,
    IDelete<IIgdVisitKey>,
    IGetData<IgdVisitDto, IIgdVisitKey>,
    IListData<IgdVisitDto, Periode>
{
    IEnumerable<IgdVisitDto> ListAktif();
}

public class IgdVisitDal : IIgdVisitDal
{
    private readonly DatabaseOptions _opt;

    public IgdVisitDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IgdVisitDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_IgdVisit (
                IgdVisitId, DaftarDateTime,
                VisitorName, VisitorGender, VisitorTglLahir, VisitorKontak,
                DokterId, DokterName,
                HasTriage, TriageMethod, TriageLevel, TriageColor, LastTriageAt, NextReTriageAt,
                AdministrativeState, RegId, PasienId, PasienName,
                RedirectRajalId, RedirectDateTime, RedirectReason,
                BedIgdId,
                DischargeUser, DischargeDateTime,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @IgdVisitId, @DaftarDateTime,
                @VisitorName, @VisitorGender, @VisitorTglLahir, @VisitorKontak,
                @DokterId, @DokterName,
                @HasTriage, @TriageMethod, @TriageLevel, @TriageColor, @LastTriageAt, @NextReTriageAt,
                @AdministrativeState, @RegId, @PasienId, @PasienName,
                @RedirectRajalId, @RedirectDateTime, @RedirectReason,
                @BedIgdId,
                @DischargeUser, @DischargeDateTime,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(IgdVisitDto dto)
    {
        const string sql = """
            UPDATE BILRG_IgdVisit
            SET DaftarDateTime = @DaftarDateTime,
                VisitorName = @VisitorName,
                VisitorGender = @VisitorGender,
                VisitorTglLahir = @VisitorTglLahir,
                VisitorKontak = @VisitorKontak,
                DokterId = @DokterId,
                DokterName = @DokterName,
                HasTriage = @HasTriage,
                TriageMethod = @TriageMethod,
                TriageLevel = @TriageLevel,
                TriageColor = @TriageColor,
                LastTriageAt = @LastTriageAt,
                NextReTriageAt = @NextReTriageAt,
                AdministrativeState = @AdministrativeState,
                RegId = @RegId,
                PasienId = @PasienId,
                PasienName = @PasienName,
                RedirectRajalId = @RedirectRajalId,
                RedirectDateTime = @RedirectDateTime,
                RedirectReason = @RedirectReason,
                BedIgdId = @BedIgdId,
                DischargeUser = @DischargeUser,
                DischargeDateTime = @DischargeDateTime,
                CrtUser = @CrtUser, CrtDate = @CrtDate,
                UpdUser = @UpdUser, UpdDate = @UpdDate,
                VodUser = @VodUser, VodDate = @VodDate
            WHERE IgdVisitId = @IgdVisitId
            """;

        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IIgdVisitKey key)
    {
        const string sql = """
            DELETE BILRG_IgdVisit WHERE IgdVisitId = @IgdVisitId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", key.IgdVisitId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IgdVisitDto GetData(IIgdVisitKey key)
    {
        var sql = SelectFromClause() + " WHERE aa.IgdVisitId = @IgdVisitId";
        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", key.IgdVisitId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<IgdVisitDto>(sql, dp);
    }

    public IEnumerable<IgdVisitDto> ListData(Periode filter)
    {
        var sql = SelectFromClause() + """
            
             WHERE aa.DaftarDateTime BETWEEN @Tgl1 AND @Tgl2
               AND aa.VodDate = @VodDate
             ORDER BY aa.DaftarDateTime
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", filter.Tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", filter.Tgl2, SqlDbType.DateTime);
        dp.AddParam("@VodDate", new DateTime(3000, 1, 1), SqlDbType.DateTime);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<IgdVisitDto>(sql, dp);
    }

    public IEnumerable<IgdVisitDto> ListAktif()
    {
        var sql = SelectFromClause() + """
            
             WHERE aa.AdministrativeState IN ('DAFTAR', 'REGISTERED')
               AND aa.VodDate = @VodDate
             ORDER BY aa.DaftarDateTime
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@VodDate", new DateTime(3000, 1, 1), SqlDbType.DateTime);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<IgdVisitDto>(sql, dp);
    }

    private static string SelectFromClause() => """
        SELECT
            aa.IgdVisitId, aa.DaftarDateTime,
            aa.VisitorName, aa.VisitorGender, aa.VisitorTglLahir, aa.VisitorKontak,
            aa.DokterId, aa.DokterName,
            aa.HasTriage, aa.TriageMethod, aa.TriageLevel, aa.TriageColor, aa.LastTriageAt, aa.NextReTriageAt,
            aa.AdministrativeState, aa.RegId, aa.PasienId, aa.PasienName,
            aa.RedirectRajalId, aa.RedirectDateTime, aa.RedirectReason,
            aa.BedIgdId,
            aa.DischargeUser, aa.DischargeDateTime,
            aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
        FROM BILRG_IgdVisit aa
        """;

    private static DynamicParameters BuildParams(IgdVisitDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", dto.IgdVisitId, SqlDbType.VarChar);
        dp.AddParam("@DaftarDateTime", dto.DaftarDateTime, SqlDbType.DateTime);

        dp.AddParam("@VisitorName", dto.VisitorName, SqlDbType.VarChar);
        dp.AddParam("@VisitorGender", dto.VisitorGender, SqlDbType.VarChar);
        dp.AddParam("@VisitorTglLahir", dto.VisitorTglLahir, SqlDbType.DateTime);
        dp.AddParam("@VisitorKontak", dto.VisitorKontak, SqlDbType.VarChar);

        dp.AddParam("@DokterId", dto.DokterId, SqlDbType.VarChar);
        dp.AddParam("@DokterName", dto.DokterName, SqlDbType.VarChar);

        dp.AddParam("@HasTriage", dto.HasTriage, SqlDbType.Bit);
        dp.AddParam("@TriageMethod", dto.TriageMethod, SqlDbType.VarChar);
        dp.AddParam("@TriageLevel", dto.TriageLevel, SqlDbType.VarChar);
        dp.AddParam("@TriageColor", dto.TriageColor, SqlDbType.VarChar);
        dp.AddParam("@LastTriageAt", dto.LastTriageAt, SqlDbType.DateTime);
        dp.AddParam("@NextReTriageAt", dto.NextReTriageAt, SqlDbType.DateTime);

        dp.AddParam("@AdministrativeState", dto.AdministrativeState, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);

        dp.AddParam("@RedirectRajalId", dto.RedirectRajalId, SqlDbType.VarChar);
        dp.AddParam("@RedirectDateTime", dto.RedirectDateTime, SqlDbType.DateTime);
        dp.AddParam("@RedirectReason", dto.RedirectReason, SqlDbType.VarChar);

        dp.AddParam("@BedIgdId", dto.BedIgdId, SqlDbType.VarChar);

        dp.AddParam("@DischargeUser", dto.DischargeUser, SqlDbType.VarChar);
        dp.AddParam("@DischargeDateTime", dto.DischargeDateTime, SqlDbType.DateTime);

        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
