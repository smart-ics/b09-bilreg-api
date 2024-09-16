using System.Data;
using System.Data.SqlClient;
using Dapper;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.Helpers;

public class ParamNoDal : INunaCounterDal, INunaCounterDecDal
{
    private readonly DatabaseOptions _opt;
    public ParamNoDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public string? GetNewHexNumber(string prefix)
    {
        throw new NotImplementedException();
    }
    public void UpdateNewHexNumber(string prefix, string hexValue)
    {
        throw new NotImplementedException();
    }
    public void InsertNewHexNumber(string prefix, string hexValue)
    {
        throw new NotImplementedException();
    }

    [PublicAPI]
    private class ParamNoDto
    {
        public string Prefix { get; set; }
        public string HexVal { get; set; }
    }

    public long GetNewDecNumber(string anchor)
    {
        const string sql = @"
            SELECT fn_value
            FROM tz_parameter_no
            WHERE fs_kd_parameter = @fs_kd_parameter";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_parameter", anchor, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var dr = conn.ReadSingle<long>(sql, dp);
        return dr;
    }

    public void UpdateNewDecNumber(string anchor, long decValue)
    {
        const string sql = @"
            UPDATE tz_parameter_no
            SET fn_value = @fn_value
            WHERE fs_kd_parameter = @fs_kd_parameter";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_parameter", anchor, SqlDbType.VarChar);
        dp.AddParam("@fn_value", decValue, SqlDbType.BigInt);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void InsertNewDecNumber(string anchor, long decValue)
    {
        const string sql = @"
            INSERT INTO tz_parameter_no(fs_kd_parameter, fn_value)
            VALUES (@fs_kd_parameter, @fn_value)";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_parameter", anchor, SqlDbType.VarChar);
        dp.AddParam("@fn_value", decValue, SqlDbType.BigInt);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
}