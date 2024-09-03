using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.LayananSub.LayananAgg;

public class LayananDal : ILayananDal
{
    private readonly DatabaseOptions _opt;

    public LayananDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(LayananModel model)
    {
        const string sql = @"
            INSERT INTO ta_layanan (fs_kd_layanan, fs_nm_layanan)
            VALUES(@fs_kd_layanan, @fs_nm_layanan)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan", model.LayananId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_layanan", model.LayananName, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(LayananModel model)
    {
        const string sql = @"
            UPDATE ta_layanan
            SET fs_nm_layanan = @fs_nm_layanan
            WHERE fs_kd_layanan = @fs_kd_layanan ";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan", model.LayananId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_layanan", model.LayananName, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ILayananKey key)
    {
        const string sql = @"
            DELETE FROM ta_layanan
            WHERE fs_kd_layanan = @fs_kd_layanan ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan", key.LayananId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public LayananModel GetData(ILayananKey key)
    {
        const string sql = @"
            SELECT fs_kd_layanan, fs_nm_layanan
            FROM ta_layanan 
            WHERE fs_kd_layanan = @fs_kd_layanan ";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan", key.LayananId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QuerySingle<LayananDto>(sql, dp);
    }

    public IEnumerable<LayananModel> ListData()
    {
        const string sql = @"
            SELECT fs_kd_layanan, fs_nm_layanan
            FROM ta_layanan ";
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<LayananDto>(sql);
    }
}

public class LayananDto() : LayananModel(string.Empty, string.Empty)
{
    public string fs_kd_layanan
    {
        get => LayananId; 
        set => LayananId = value;
    }
    public string fs_nm_layanan
    {
        get => LayananName; 
        set => LayananName = value;
    }
}