using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BillContext.TindakanSub.TarifAgg;
using Bilreg.Domain.BillContext.TindakanSub.TarifAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.TarifAgg;

public class TarifDal: ITarifDal
{
    private readonly DatabaseOptions _opt;

    public TarifDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TarifModel model)
    {
        const string sql = @"
            INSERT INTO ta_tarif (fs_kd_tarif, fs_nm_tarif)
            VALUES (@fs_kd_tarif, @fs_nm_tarif)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tarif", model.TarifId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_tarif", model.TarifName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TarifModel model)
    {
        const string sql = @"
            UPDATE ta_tarif
            SET fs_nm_tarif = @fs_nm_tarif
            WHERE fs_kd_tarif = @fs_kd_tarif";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tarif", model.TarifId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_tarif", model.TarifName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(TarifModel key)
    {
        const string sql = @"
            DELETE FROM ta_tarif
            WHERE fs_kd_tarif = @fs_kd_tarif";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tarif", key.TarifId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TarifModel GetData(ITarifKey key)
    {
        const string sql = @"
            SELECT fs_kd_tarif, fs_nm_tarif
            FROM ta_tarif
            WHERE fs_kd_tarif = @fs_kd_tarif";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tarif", key.TarifId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TarifDto>(sql, dp);
    }

    public IEnumerable<TarifModel> ListData()
    {
        const string sql = @"
            SELECT fs_kd_tarif, fs_nm_tarif
            FROM ta_tarif";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TarifDto>(sql);
    }
}

public class TarifDto() : TarifModel(String.Empty, String.Empty)
{
    public string fs_kd_tarif { get => TarifId; set => TarifId = value; }
    public string fs_nm_tarif { get => TarifName; set => TarifName = value; }
}