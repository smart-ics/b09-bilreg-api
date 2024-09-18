using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BillContext.TindakanSub.GrupTarifAgg;
using Bilreg.Domain.BillContext.TindakanSub.GrupTarifAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.GrupTarifAgg;

public class GrupTarifDal: IGrupTarifDal
{
    private readonly DatabaseOptions _opt;

    public GrupTarifDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(GrupTarifModel model)
    {
        const string sql = @"
            INSERT INTO ta_grup_tarif (fs_kd_grup_tarif, fs_nm_grup_tarif)
            VALUES (@fs_kd_grup_tarif, @fs_nm_grup_tarif)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_tarif", model.GrupTarifId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_tarif", model.GrupTarifName, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(GrupTarifModel model)
    {
        const string sql = @"
            UPDATE ta_grup_tarif
            SET fs_nm_grup_tarif = @fs_nm_grup_tarif
            WHERE fs_kd_grup_tarif = @fs_kd_grup_tarif";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_tarif", model.GrupTarifId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_tarif", model.GrupTarifName, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IGrupTarifKey key)
    {
        const string sql = @"
            DELETE FROM ta_grup_tarif
            WHERE fs_kd_grup_tarif = @fs_kd_grup_tarif";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_tarif", key.GrupTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public GrupTarifModel GetData(IGrupTarifKey key)
    {
        const string sql = @"
            SELECT fs_kd_grup_tarif, fs_nm_grup_tarif
            FROM ta_grup_tarif
            WHERE fs_kd_grup_tarif = @fs_kd_grup_tarif";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_tarif", key.GrupTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<GrupTarifDto>(sql, dp);
    }

    public IEnumerable<GrupTarifModel> ListData()
    {
        const string sql = @"
            SELECT fs_kd_grup_tarif, fs_nm_grup_tarif
            FROM ta_grup_tarif";
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<GrupTarifDto>(sql);
    }
}

public class GrupTarifDto() : GrupTarifModel(string.Empty, string.Empty)
{
    public string fs_kd_grup_tarif { get => GrupTarifId; set => GrupTarifId = value; }
    public string fs_nm_grup_tarif { get => GrupTarifName; set => GrupTarifName = value; }
}