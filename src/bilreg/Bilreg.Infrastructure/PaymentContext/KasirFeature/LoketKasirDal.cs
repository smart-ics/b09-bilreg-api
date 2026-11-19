using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public class LoketKasirDal : ILoketKasirDal
{
    private readonly DatabaseOptions _opt;

    public LoketKasirDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<LoketKasirDto> ListAktif()
    {
        const string sql = """
            SELECT
                RTRIM(LoketId) AS LoketId,
                RTRIM(NamaLoket) AS NamaLoket,
                RTRIM(CoaId) AS CoaId,
                RTRIM(NamaCoa) AS NamaCoa,
                IsAktif,
                RTRIM(Keterangan) AS Keterangan
            FROM BILRG_LoketKasir
            WHERE IsAktif = 1
            ORDER BY LoketId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<LoketKasirDto>(sql);
    }

    public IEnumerable<LoketKasirDto> ListData()
    {
        const string sql = """
            SELECT
                RTRIM(LoketId) AS LoketId,
                RTRIM(NamaLoket) AS NamaLoket,
                RTRIM(CoaId) AS CoaId,
                RTRIM(NamaCoa) AS NamaCoa,
                IsAktif,
                RTRIM(Keterangan) AS Keterangan
            FROM BILRG_LoketKasir
            ORDER BY LoketId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<LoketKasirDto>(sql);
    }

    public LoketKasirDto? GetData(string loketId)
    {
        const string sql = """
            SELECT
                RTRIM(LoketId) AS LoketId,
                RTRIM(NamaLoket) AS NamaLoket,
                RTRIM(CoaId) AS CoaId,
                RTRIM(NamaCoa) AS NamaCoa,
                IsAktif,
                RTRIM(Keterangan) AS Keterangan
            FROM BILRG_LoketKasir
            WHERE LoketId = @LoketId
            """;

        var dp = new DynamicParameters();
        dp.Add("@LoketId", loketId, DbType.String);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QuerySingleOrDefault<LoketKasirDto>(sql, dp);
    }
}
