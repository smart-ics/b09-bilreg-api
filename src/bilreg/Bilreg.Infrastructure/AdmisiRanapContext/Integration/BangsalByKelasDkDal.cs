using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.Integration;

public interface IBangsalByKelasDkDal
{
    IEnumerable<BangsalReff> ListByKelasDkId(string kelasDkId);
}

public class BangsalByKelasDkDal : IBangsalByKelasDkDal
{
    private readonly DatabaseOptions _opt;

    public BangsalByKelasDkDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public IEnumerable<BangsalReff> ListByKelasDkId(string kelasDkId)
    {
        const string sql = """
            SELECT DISTINCT
                bs.fs_kd_bangsal AS BangsalId,
                bs.fs_nm_bangsal AS BangsalName
            FROM ta_kamar km
            INNER JOIN ta_bangsal bs ON km.fs_kd_bangsal = bs.fs_kd_bangsal
            INNER JOIN ta_kelas kl ON km.fs_kd_kelas = kl.fs_kd_kelas
            INNER JOIN ta_kelas_dk dk ON kl.fs_kd_kelas_dk = dk.fs_kd_kelas_dk
            WHERE dk.fs_kd_kelas_dk = @kelasDkId
            ORDER BY bs.fs_nm_bangsal
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@kelasDkId", kelasDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Read<BangsalRow>(sql, dp) ?? [];
        return rows.Select(r => new BangsalReff(r.BangsalId, r.BangsalName)).ToList();
    }

    private sealed record BangsalRow(string BangsalId, string BangsalName);
}
