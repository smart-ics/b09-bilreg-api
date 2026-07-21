using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.RnaServiceExecutionFeature;

public interface IRnaServiceExecutionDal : IInsert<RnaServiceExecutionDto>, IUpdate<RnaServiceExecutionDto>,
    IDelete<IRnaServiceExecutionKey>,
    IGetData<RnaServiceExecutionDto, IRnaServiceExecutionKey>,
    IListData<RnaServiceExecutionDto>
{
    int UpdateConditional(RnaServiceExecutionDto dto, int expectedVersion);
}

public sealed class RnaServiceExecutionDal : IRnaServiceExecutionDal
{
    private const string SelectColumns = """
        aa.ServiceExecutionId, aa.RegistrationId, aa.PatientId, aa.CareContextId,
        aa.ResponsibleWardId, aa.ExecutionSource, aa.ClinicalOrderId, aa.OrderOccurrenceId,
        aa.FulfilmentObligationId, aa.SourceContext, aa.SourceFactId, aa.SourceRevision,
        aa.AssignedPerformerId, aa.WorkStatus, aa.HasExecutionAuthority,
        aa.AuthorityBasisReference, aa.RequiresSubsequentAuthorization,
        aa.AuthorizationDueAt, aa.SubsequentAuthorizationStatus, aa.Version,
        aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
        """;

    private readonly DatabaseOptions _opt;

    public RnaServiceExecutionDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(RnaServiceExecutionDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_RnaServiceExecution (
                ServiceExecutionId, RegistrationId, PatientId, CareContextId,
                ResponsibleWardId, ExecutionSource, ClinicalOrderId, OrderOccurrenceId,
                FulfilmentObligationId, SourceContext, SourceFactId, SourceRevision,
                AssignedPerformerId, WorkStatus, HasExecutionAuthority,
                AuthorityBasisReference, RequiresSubsequentAuthorization,
                AuthorizationDueAt, SubsequentAuthorizationStatus, Version,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @ServiceExecutionId, @RegistrationId, @PatientId, @CareContextId,
                @ResponsibleWardId, @ExecutionSource, @ClinicalOrderId, @OrderOccurrenceId,
                @FulfilmentObligationId, @SourceContext, @SourceFactId, @SourceRevision,
                @AssignedPerformerId, @WorkStatus, @HasExecutionAuthority,
                @AuthorityBasisReference, @RequiresSubsequentAuthorization,
                @AuthorizationDueAt, @SubsequentAuthorizationStatus, @Version,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public void Update(RnaServiceExecutionDto dto) => UpdateConditional(dto, -1);

    public int UpdateConditional(RnaServiceExecutionDto dto, int expectedVersion)
    {
        const string sql = """
            UPDATE BILRG_RnaServiceExecution
            SET
                RegistrationId = @RegistrationId,
                PatientId = @PatientId,
                CareContextId = @CareContextId,
                ResponsibleWardId = @ResponsibleWardId,
                ExecutionSource = @ExecutionSource,
                ClinicalOrderId = @ClinicalOrderId,
                OrderOccurrenceId = @OrderOccurrenceId,
                FulfilmentObligationId = @FulfilmentObligationId,
                SourceContext = @SourceContext,
                SourceFactId = @SourceFactId,
                SourceRevision = @SourceRevision,
                AssignedPerformerId = @AssignedPerformerId,
                WorkStatus = @WorkStatus,
                HasExecutionAuthority = @HasExecutionAuthority,
                AuthorityBasisReference = @AuthorityBasisReference,
                RequiresSubsequentAuthorization = @RequiresSubsequentAuthorization,
                AuthorizationDueAt = @AuthorizationDueAt,
                SubsequentAuthorizationStatus = @SubsequentAuthorizationStatus,
                Version = @Version,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE
                ServiceExecutionId = @ServiceExecutionId
                AND (@ExpectedVersion < 0 OR Version = @ExpectedVersion)
            """;

        var dp = MapParams(dto);
        dp.AddParam("@ExpectedVersion", expectedVersion, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Execute(sql, dp);
    }

    public void Delete(IRnaServiceExecutionKey key)
    {
        const string sql = """
            DELETE FROM BILRG_RnaServiceExecution
            WHERE ServiceExecutionId = @ServiceExecutionId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ServiceExecutionId", key.ServiceExecutionId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public RnaServiceExecutionDto GetData(IRnaServiceExecutionKey key)
    {
        var sql = $"""
            SELECT
                {SelectColumns}
            FROM
                BILRG_RnaServiceExecution aa
            WHERE
                aa.ServiceExecutionId = @ServiceExecutionId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ServiceExecutionId", key.ServiceExecutionId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<RnaServiceExecutionDto>(sql, dp);
    }

    public IEnumerable<RnaServiceExecutionDto> ListData()
    {
        var sql = $"""
            SELECT
                {SelectColumns}
            FROM
                BILRG_RnaServiceExecution aa
            ORDER BY
                aa.UpdDate DESC,
                aa.ServiceExecutionId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RnaServiceExecutionDto>(sql);
    }

    private static DynamicParameters MapParams(RnaServiceExecutionDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@ServiceExecutionId", dto.ServiceExecutionId, SqlDbType.VarChar);
        dp.AddParam("@RegistrationId", dto.RegistrationId, SqlDbType.VarChar);
        dp.AddParam("@PatientId", dto.PatientId, SqlDbType.VarChar);
        dp.AddParam("@CareContextId", dto.CareContextId, SqlDbType.VarChar);
        dp.AddParam("@ResponsibleWardId", dto.ResponsibleWardId, SqlDbType.VarChar);
        dp.AddParam("@ExecutionSource", dto.ExecutionSource, SqlDbType.Int);
        dp.AddParam("@ClinicalOrderId", dto.ClinicalOrderId, SqlDbType.VarChar);
        dp.AddParam("@OrderOccurrenceId", dto.OrderOccurrenceId, SqlDbType.VarChar);
        dp.AddParam("@FulfilmentObligationId", dto.FulfilmentObligationId, SqlDbType.VarChar);
        dp.AddParam("@SourceContext", dto.SourceContext, SqlDbType.VarChar);
        dp.AddParam("@SourceFactId", dto.SourceFactId, SqlDbType.VarChar);
        dp.AddParam("@SourceRevision", dto.SourceRevision, SqlDbType.Int);
        dp.AddParam("@AssignedPerformerId", dto.AssignedPerformerId, SqlDbType.VarChar);
        dp.AddParam("@WorkStatus", dto.WorkStatus, SqlDbType.Int);
        dp.AddParam("@HasExecutionAuthority", dto.HasExecutionAuthority, SqlDbType.Bit);
        dp.AddParam("@AuthorityBasisReference", dto.AuthorityBasisReference, SqlDbType.VarChar);
        dp.AddParam("@RequiresSubsequentAuthorization", dto.RequiresSubsequentAuthorization, SqlDbType.Bit);
        dp.AddParam("@AuthorizationDueAt", dto.AuthorizationDueAt, SqlDbType.DateTime);
        dp.AddParam("@SubsequentAuthorizationStatus", dto.SubsequentAuthorizationStatus, SqlDbType.Int);
        dp.AddParam("@Version", dto.Version, SqlDbType.Int);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
