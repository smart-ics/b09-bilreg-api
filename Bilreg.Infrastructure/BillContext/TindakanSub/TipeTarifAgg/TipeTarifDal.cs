using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;
using Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.TipeTarifAgg;

public class TipeTarifDal : ITipeTarifDal
{
    private readonly DatabaseOptions _opt;

    public TipeTarifDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(TipeTarifModel model)
    {
        const string sql = @"
           INSERT INTO ta_tarif_tipe
                (fs_kd_tarif_tipe, fs_nm_tarif_tipe, fb_aktif, fs_no_urut)
           VALUES 
               (@fs_kd_tarif_tipe, @fs_nm_tarif_tipe, @fb_aktif, @fs_no_urut)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tarif_tipe", model.TipeTarifId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_tarif_tipe", model.TipeTarifName, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fs_no_urut", model.NoUrut, SqlDbType.Decimal);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TipeTarifModel model)
    {
        const string sql = @"
           UPDATE 
               ta_tarif_tipe
           SET
               fs_nm_tarif_tipe = @fs_nm_tarif_tipe,
               fb_aktif = @fb_aktif,
               fs_no_urut = @fs_no_urut
           WHERE
               fs_kd_tarif_tipe = @fs_kd_tarif_tipe";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tarif_tipe", model.TipeTarifId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_tarif_tipe", model.TipeTarifName, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fs_no_urut", model.NoUrut, SqlDbType.Decimal);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ITipeTarifKey key)
    {
        const string sql = @"
           DELETE FROM 
                ta_tarif_tipe
           WHERE
               fs_kd_tarif_tipe = @fs_kd_tarif_tipe";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tarif_tipe", key.TipeTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TipeTarifModel GetData(ITipeTarifKey key)
    {
        const string sql = @"
           SELECT
               fs_kd_tarif_tipe,
               fs_nm_tarif_tipe,
               fb_aktif,
               fs_no_urut
           FROM 
               ta_tarif_tipe
           WHERE
               fs_kd_tarif_tipe = @fs_kd_tarif_tipe";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tarif_tipe", key.TipeTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<TipeTarifDto>(sql, dp);
        return result;
    }

    public IEnumerable<TipeTarifModel> ListData()
    {
        const string sql = @"
           SELECT
               fs_kd_tarif_tipe,
               fs_nm_tarif_tipe,
               fb_aktif,
               fs_no_urut
           FROM 
               ta_tarif_tipe";
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TipeTarifDto>(sql);
        
    }
}

public class TipeTarifDto() : TipeTarifModel("","")
{
    public void SetDataTipeTarif()
    {
        TipeTarifId = "A";
        TipeTarifName = "B";
        IsAktif = false;
        NoUrut = 0;
    }
    
    public string fs_kd_tarif_tipe { get => TipeTarifId; set => TipeTarifId = value; }
    public string fs_nm_tarif_tipe { get => TipeTarifName; set => TipeTarifName = value; }
    public bool fb_aktif { get => IsAktif; set => IsAktif = value; }
    public decimal fs_no_urut { get => NoUrut; set => NoUrut = value; }
}