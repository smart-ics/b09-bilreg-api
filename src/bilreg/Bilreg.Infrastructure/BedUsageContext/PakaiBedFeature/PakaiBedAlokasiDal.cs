using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;

public interface IPakaiBedAlokasiDal :
    IInsert<PakaiBedAlokasiDto>,
    IUpdate<PakaiBedAlokasiDto>,
    IGetData<PakaiBedAlokasiDto, IPakaiBedAlokasiKey>
{
    int UpdateConditional(PakaiBedAlokasiDto dto, int expectedVersion);
}

public class PakaiBedAlokasiDal : IPakaiBedAlokasiDal
{
    private const string SelectFrom = """
        SELECT
            aa.PakaiBedId, aa.Version, aa.RegId, aa.PasienId,
            aa.BangsalId, aa.KamarId, aa.BedId, aa.WaitingListId, aa.RequestId,
            aa.PakaiBedPurpose, aa.OccupantRole, aa.PakaiBedStatus,
            aa.ProposedAt, aa.StartedAt, aa.AssignedBy, aa.BedAssignabilityEvidenceId,
            aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate,
            aa.VodUser, aa.VodDate
        FROM BILRG_RnaPakaiBedAlokasi aa
        """;

    private readonly DatabaseOptions _opt;
    public PakaiBedAlokasiDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(PakaiBedAlokasiDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_RnaPakaiBedAlokasi(
                PakaiBedId, Version, RegId, PasienId, BangsalId, KamarId, BedId,
                WaitingListId, RequestId, PakaiBedPurpose, OccupantRole, PakaiBedStatus,
                ProposedAt, StartedAt, AssignedBy, BedAssignabilityEvidenceId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES(
                @PakaiBedId, @Version, @RegId, @PasienId, @BangsalId, @KamarId, @BedId,
                @WaitingListId, @RequestId, @PakaiBedPurpose, @OccupantRole, @PakaiBedStatus,
                @ProposedAt, @StartedAt, @AssignedBy, @BedAssignabilityEvidenceId,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(PakaiBedAlokasiDto dto)
    {
        const string sql = """
            UPDATE BILRG_RnaPakaiBedAlokasi SET
                Version = @Version, RegId = @RegId, PasienId = @PasienId,
                BangsalId = @BangsalId, KamarId = @KamarId, BedId = @BedId,
                WaitingListId = @WaitingListId, RequestId = @RequestId,
                PakaiBedPurpose = @PakaiBedPurpose, OccupantRole = @OccupantRole,
                PakaiBedStatus = @PakaiBedStatus, ProposedAt = @ProposedAt,
                StartedAt = @StartedAt, AssignedBy = @AssignedBy,
                BedAssignabilityEvidenceId = @BedAssignabilityEvidenceId,
                UpdUser = @UpdUser, UpdDate = @UpdDate,
                VodUser = @VodUser, VodDate = @VodDate
            WHERE PakaiBedId = @PakaiBedId
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public int UpdateConditional(PakaiBedAlokasiDto dto, int expectedVersion)
    {
        const string sql = """
            UPDATE BILRG_RnaPakaiBedAlokasi SET
                Version = @Version, RegId = @RegId, PasienId = @PasienId,
                BangsalId = @BangsalId, KamarId = @KamarId, BedId = @BedId,
                WaitingListId = @WaitingListId, RequestId = @RequestId,
                PakaiBedPurpose = @PakaiBedPurpose, OccupantRole = @OccupantRole,
                PakaiBedStatus = @PakaiBedStatus, ProposedAt = @ProposedAt,
                StartedAt = @StartedAt, AssignedBy = @AssignedBy,
                BedAssignabilityEvidenceId = @BedAssignabilityEvidenceId,
                UpdUser = @UpdUser, UpdDate = @UpdDate,
                VodUser = @VodUser, VodDate = @VodDate
            WHERE PakaiBedId = @PakaiBedId AND Version = @ExpectedVersion
            """;
        var dp = BuildParams(dto);
        dp.AddParam("@ExpectedVersion", expectedVersion, SqlDbType.Int);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Execute(sql, dp);
    }

    public PakaiBedAlokasiDto GetData(IPakaiBedAlokasiKey key)
    {
        var sql = $"{SelectFrom}\nWHERE aa.PakaiBedId = @PakaiBedId";
        var dp = new DynamicParameters();
        dp.AddParam("@PakaiBedId", key.PakaiBedId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PakaiBedAlokasiDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(PakaiBedAlokasiDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@PakaiBedId", dto.PakaiBedId, SqlDbType.VarChar);
        dp.AddParam("@Version", dto.Version, SqlDbType.Int);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@BangsalId", dto.BangsalId, SqlDbType.VarChar);
        dp.AddParam("@KamarId", dto.KamarId, SqlDbType.VarChar);
        dp.AddParam("@BedId", dto.BedId, SqlDbType.VarChar);
        dp.AddParam("@WaitingListId", dto.WaitingListId, SqlDbType.VarChar);
        dp.AddParam("@RequestId", dto.RequestId, SqlDbType.VarChar);
        dp.AddParam("@PakaiBedPurpose", dto.PakaiBedPurpose, SqlDbType.Int);
        dp.AddParam("@OccupantRole", dto.OccupantRole, SqlDbType.Int);
        dp.AddParam("@PakaiBedStatus", dto.PakaiBedStatus, SqlDbType.Int);
        dp.AddParam("@ProposedAt", dto.ProposedAt, SqlDbType.DateTime);
        dp.AddParam("@StartedAt", dto.StartedAt, SqlDbType.DateTime);
        dp.AddParam("@AssignedBy", dto.AssignedBy, SqlDbType.VarChar);
        dp.AddParam("@BedAssignabilityEvidenceId", dto.BedAssignabilityEvidenceId, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
