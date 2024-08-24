using System.Data;
using System.Data.SqlClient;
using Dapper;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.Helpers;

public class ParamNoDal : INunaCounterDal
{
    private readonly DatabaseOptions _opt;
    public ParamNoDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public string? GetNewHexNumber(string prefix)
    {
        const string sql = @"
            SELECT
                Prefix, HexVal
            FROM
                NERS_ParamNo
            WHERE
                Prefix = @Prefix ";

        var dp = new DynamicParameters();
        dp.AddParam("@Prefix", prefix, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var dr = conn.ReadSingle<ParamNoDto>(sql, dp);
        return dr?.HexVal;
    }

    public void UpdateNewHexNumber(string prefix, string hexValue)
    {
        const string sql = @"
            UPDATE
                NERS_ParamNo
            SET
                HexVal = @HexVal
            WHERE
                Prefix = @Prefix ";

        var dp = new DynamicParameters();
        dp.AddParam("@Prefix", prefix, SqlDbType.VarChar);
        dp.AddParam("@HexVal", hexValue, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void InsertNewHexNumber(string prefix, string hexValue)
    {


        const string sql = @"
            INSERT INTO 
                NERS_ParamNo (
                    Prefix, HexVal)
            VALUES (
                    @Prefix, @HexVal)";

        var dp = new DynamicParameters();
        dp.AddParam("@Prefix", prefix, SqlDbType.VarChar);
        dp.AddParam("@HexVal", hexValue, SqlDbType.VarChar);


        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    [PublicAPI]
    private class ParamNoDto
    {
        public string Prefix { get; set; }
        public string HexVal { get; set; }
    }
}