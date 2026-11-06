using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.DataTypeExtension;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;

public class TrsDirectDal : ITrsDirectDal
{
    private readonly DatabaseOptions _opt;

    public TrsDirectDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void UpdateTimestamp(string trsId, DateTime tglTrs, decimal genKeu)
    {
        const string sql = """
            UPDATE ta_trs_direct
            SET
                FD_TGL_TRS = @tgl,
                FS_JAM_TRS = @jam,
                fd_tgl_jam_trs = @tglJam,
                fn_genkeu = @genKeu
            WHERE
                FS_KD_TRS = @trsId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@trsId", trsId, SqlDbType.VarChar);
        dp.AddParam("@tgl", tglTrs.ToString(DateFormatEnum.YMD), SqlDbType.VarChar);
        dp.AddParam("@jam", tglTrs.ToString(DateFormatEnum.HMS), SqlDbType.VarChar);
        dp.AddParam("@tglJam", tglTrs.ToString(DateFormatEnum.YMD_HMS), SqlDbType.VarChar);
        dp.AddParam("@genKeu", genKeu, SqlDbType.Decimal);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
}
