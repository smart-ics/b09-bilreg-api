using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

public sealed class TransferReceivableService : ITransferReceivableService
{
    private readonly DatabaseOptions _opt;

    public TransferReceivableService(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Transfer(string sourceRegId, string targetRegId)
    {
        if (string.IsNullOrWhiteSpace(sourceRegId))
            throw new ArgumentException("Source Registrasi tidak boleh kosong.", nameof(sourceRegId));

        if (string.IsNullOrWhiteSpace(targetRegId))
            throw new ArgumentException("Target Registrasi tidak boleh kosong.", nameof(targetRegId));

        const string sql = """
            UPDATE t_bp_piutang_hdr
            SET fs_keterangan = STUFF(fs_keterangan, 1, LEN(@SourceRegId), @TargetRegId)
            WHERE LEFT(fs_keterangan, LEN(@SourceRegId)) = @SourceRegId
              AND fd_tgl_void = '3000-01-01'
            """;

        var dp = new DynamicParameters();
        dp.Add("@SourceRegId", sourceRegId, DbType.String, size: 10);
        dp.Add("@TargetRegId", targetRegId, DbType.String, size: 10);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
}
