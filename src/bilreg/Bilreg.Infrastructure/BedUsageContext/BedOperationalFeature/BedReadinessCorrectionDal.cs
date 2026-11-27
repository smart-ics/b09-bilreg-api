using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.Shared;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.BedOperationalFeature;

public interface IBedReadinessCorrectionDal :
    IInsertBulk<BedReadinessCorrectionDto>,
    IListData<BedReadinessCorrectionDto, IBedOperationalKey>
{
}

public class BedReadinessCorrectionDal : IBedReadinessCorrectionDal
{
    private readonly DatabaseOptions _opt;
    private readonly ISqlConnectionScope _connectionScope;

    public BedReadinessCorrectionDal(IOptions<DatabaseOptions> opt, ISqlConnectionScope? connectionScope = null)
    {
        _opt = opt.Value;
        _connectionScope = connectionScope ?? new SqlConnectionScope(opt);
    }

    private SqlConnection GetConnection() => _connectionScope.GetConnection();

    public void Insert(IEnumerable<BedReadinessCorrectionDto> listModel)
    {
        var rows = listModel.ToList();
        if (rows.Count == 0)
            return;

        const string sql = """
            INSERT INTO BILRG_RnaBedReadinessCorrection (
                CorrectionId, BedId, OriginalTransactionId,
                ReplacementTransactionId, ActorId, Reason,
                OccurredAt, RecordedAt, RequestId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            ) VALUES (
                @CorrectionId, @BedId, @OriginalTransactionId,
                @ReplacementTransactionId, @ActorId, @Reason,
                @OccurredAt, @RecordedAt, @RequestId,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate
            );
            """;
        var conn = GetConnection();
        conn.Execute(sql, rows);
    }

    public IEnumerable<BedReadinessCorrectionDto> ListData(
        IBedOperationalKey filter)
    {
        const string sql = """
            SELECT
                CorrectionId, BedId, OriginalTransactionId,
                ReplacementTransactionId, ActorId, Reason,
                OccurredAt, RecordedAt, RequestId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM BILRG_RnaBedReadinessCorrection
            WHERE BedId = @BedId
            ORDER BY OccurredAt, CorrectionId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@BedId", filter.BedId, SqlDbType.VarChar);
        var conn = GetConnection();
        return conn.Read<BedReadinessCorrectionDto>(sql, dp);
    }

    private static readonly string[] Columns =
    [
        "CorrectionId", "BedId", "OriginalTransactionId",
        "ReplacementTransactionId", "ActorId", "Reason",
        "OccurredAt", "RecordedAt", "RequestId",
        "CrtUser", "CrtDate", "UpdUser", "UpdDate", "VodUser", "VodDate"
    ];
}
