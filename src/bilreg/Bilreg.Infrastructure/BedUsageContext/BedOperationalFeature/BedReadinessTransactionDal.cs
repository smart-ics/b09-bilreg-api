using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.Shared;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.BedOperationalFeature;

public interface IBedReadinessTransactionDal :
    IInsertBulk<BedReadinessTransactionDto>,
    IListData<BedReadinessTransactionDto, IBedOperationalKey>
{
}

public class BedReadinessTransactionDal : IBedReadinessTransactionDal
{
    private readonly DatabaseOptions _opt;
    private readonly ISqlConnectionScope _connectionScope;

    public BedReadinessTransactionDal(IOptions<DatabaseOptions> opt, ISqlConnectionScope? connectionScope = null)
    {
        _opt = opt.Value;
        _connectionScope = connectionScope ?? new SqlConnectionScope(opt);
    }

    private SqlConnection GetConnection() => _connectionScope.GetConnection();

    public void Insert(IEnumerable<BedReadinessTransactionDto> listModel)
    {
        var rows = listModel.ToList();
        if (rows.Count == 0)
            return;

        const string sql = """
            INSERT INTO BILRG_RnaBedReadinessTransaction (
                TransactionId, BedId, NewStatus, RestrictionType,
                Reason, EvidenceReference, ResponsibleActorId, VerifiedByActorId,
                OccurredAt, RecordedAt, SourceFactId, RequestId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            ) VALUES (
                @TransactionId, @BedId, @NewStatus, @RestrictionType,
                @Reason, @EvidenceReference, @ResponsibleActorId, @VerifiedByActorId,
                @OccurredAt, @RecordedAt, @SourceFactId, @RequestId,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate
            );
            """;
        var conn = GetConnection();
        conn.Execute(sql, rows);
    }

    public IEnumerable<BedReadinessTransactionDto> ListData(
        IBedOperationalKey filter)
    {
        const string sql = """
            SELECT
                TransactionId, BedId, NewStatus, RestrictionType,
                Reason, EvidenceReference, ResponsibleActorId, VerifiedByActorId,
                OccurredAt, RecordedAt, SourceFactId, RequestId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM BILRG_RnaBedReadinessTransaction
            WHERE BedId = @BedId
            ORDER BY OccurredAt, TransactionId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@BedId", filter.BedId, SqlDbType.VarChar);
        var conn = GetConnection();
        return conn.Read<BedReadinessTransactionDto>(sql, dp);
    }
}
