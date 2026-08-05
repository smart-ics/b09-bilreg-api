using System.Data;
using System.Data.SqlClient;
using Dapper;
using Farinv.Application.ParamContext.ParamSistemAgg;
using Farinv.Domain.Shared.Param;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Infrastructure.ParamContext;

public class ParamSistemDal : IParamSistemDal
{
    private readonly DatabaseOptions _opt;

    public ParamSistemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(ParamSistemModel model)
    {
        const string sql = @"
            INSERT INTO tz_parameter_sistem
                (fs_kd_parameter, fs_nm_parameter, fs_value)
            VALUES (@fs_kd_parameter, @fs_nm_parameter, @fs_value)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_parameter", model.ParamSistemId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_parameter", model.ParamSistemName, SqlDbType.VarChar);
        dp.AddParam("@fs_value", model.Value, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(ParamSistemModel model)
    {
        const string sql = @"
            UPDATE tz_parameter_sistem
            SET
                fs_kd_parameter = @fs_kd_parameter, 
                fs_nm_parameter = @fs_nm_parameter, 
                fs_value  = @fs_value
            WHERE   
                fs_kd_parameter = @fs_kd_parameter";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_parameter", model.ParamSistemId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_parameter", model.ParamSistemName, SqlDbType.VarChar);
        dp.AddParam("@fs_value", model.Value, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IParamSistemKey key)
    {
        const string sql = @"
            DELETE FROM tz_parameter_sistem
            WHERE fs_kd_parameter = @fs_kd_parameter";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_parameter", key.ParamSistemId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public ParamSistemModel GetData(string key)
    {
        const string sql = @"
            SELECT fs_kd_parameter, fs_nm_parameter, fs_value
            FROM tz_parameter_sistem
            WHERE fs_kd_parameter = @fs_kd_parameter ";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_parameter", key, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<ParamSistemDto>(sql, dp);
    }

    public IEnumerable<ParamSistemModel> ListData()
    {
        const string sql = @"
            SELECT fs_kd_parameter, fs_nm_parameter, fs_value 
            FROM tz_parameter_sistem";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ParamSistemDto>(sql);
    }
    
    private class ParamSistemDto() : ParamSistemModel("","","")
    {
        public string fs_kd_parameter
        {
            get => base.ParamSistemId;
            set => ParamSistemId = value;
        }
        public string fs_nm_parameter
        {
            get => ParamSistemName; 
            set => ParamSistemName = value;
        }
        public string fs_value
        {
            get => Value; 
            set => Value = value;
        }
    }
}
