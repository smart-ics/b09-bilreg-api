using System.Data.SqlClient;
using Dapper;
using Farinv.Domain.Shared.Helpers;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;

namespace Farinv.Infrastructure.ParamContext;

public class SequencerManual : ISequencerManual
{
    private readonly DatabaseOptions _opt;

    public SequencerManual(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public long GetNextNoUrut(string sequenceTag, string description)
    {
        var result = sequenceTag switch
        {
            "NOMR" => GetTzParamNo(sequenceTag, description),
            "NOREG" => GetTzParamNo(sequenceTag, description),
            "PS-POLIS" => GetTzParamNo2("PS", ""),
            _ => throw new ArgumentOutOfRangeException(nameof(sequenceTag), sequenceTag, null)
        };
        return result;
    }
    
    private long GetTzParamNo(string sequenceTag, string description)
    {
        const string sql = "EXEC sp_tz_parameter_no_getnextvalue @fs_kd_parameter, @fs_nm_parameter";
    
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var parameters = new
        {
            fs_kd_parameter = sequenceTag,
            fs_nm_parameter = description ?? sequenceTag
        };
        var newNumber = conn.ExecuteScalar<decimal>(sql, parameters);
        return Convert.ToInt64(newNumber);
    }
    private long GetTzParamNo2(string sequenceTag, string modul)
    {
        const string sql = "EXEC sp_tz_parameter_no2_getnextvalue @fs_prefix, @fs_modul";
    
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var parameters = new
        {
            fs_prefix = sequenceTag,
            fs_modul = modul
        };
        var newNumber = conn.ExecuteScalar<decimal>(sql, parameters);
        return Convert.ToInt64(newNumber);
    }
}
