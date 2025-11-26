using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BillContext.RoomChargeSub.TipeKamarAgg;
using Bilreg.Domain.BillContext.BedUsageFeature.TipeKamarAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BillContext.BedUsageFeature.TipeKamarAgg;

public class TipeKamarDal : ITipeKamarDal
{
    private readonly DatabaseOptions _opt;

    public TipeKamarDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TipeKamarModel model)
    {
        const string sql = @"
            INSERT INTO ta_kamar_tipe(
            fs_kd_kamar_tipe,
            fs_nm_kamar_tipe,
            fb_gabung,
            fb_aktif,
            fb_default_tipe,
            fb_no_urut                  
            )VALUES(
             @fs_kd_kamar_tipe,
             @fs_nm_kamar_tipe,
             @fb_gabung,
             @fb_aktif,
             @fb_default_tipe,
             @fb_no_urut    
            )
        ";
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar_tipe",model.TipeKamarId ,SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kamar_tipe",model.TipeKamarName ,SqlDbType.VarChar);
        dp.AddParam("@fb_gabung",model.IsGabung.ToString() ,SqlDbType.VarChar);
        dp.AddParam("@fb_aktif",model.IsAktif.ToString() ,SqlDbType.VarChar);
        dp.AddParam("@fb_default_tipe",model.IsDefault.ToString() ,SqlDbType.VarChar);
        dp.AddParam("@fb_no_urut",model.NoUrut.ToString() ,SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TipeKamarModel model)
    {   
        const string sql = @"
            UPDATE ta_kamar_tipe
                SET
                    fs_kd_kamar_tipe = @fs_kd_kamar_tipe,
                    fs_nm_kamar_tipe = @fs_nm_kamar_tipe,
                    fb_gabung = @fb_gabung,
                    fb_aktif = @fb_aktif,
                    fb_default_tipe = @fb_default_tipe,
                    fb_no_urut = @fb_no_urut                  
                WHERE
                    fs_kd_kamar_tipe = @fs_kd_kamar_tipe
            ";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar_tipe",model.TipeKamarId ,SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kamar_tipe",model.TipeKamarName ,SqlDbType.VarChar);
        dp.AddParam("@fb_gabung",model.IsGabung.ToString() ,SqlDbType.VarChar);
        dp.AddParam("@fb_aktif",model.IsAktif.ToString() ,SqlDbType.VarChar);
        dp.AddParam("@fb_default_tipe",model.IsDefault.ToString() ,SqlDbType.VarChar);
        dp.AddParam("@fb_no_urut",model.NoUrut.ToString() ,SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ITipeKamarKey key)
    {
        const string sql = @"
            DELETE FROM ta_kamar_tipe
            WHERE fs_kd_kamar_tipe = @fs_kd_kamar_tipe ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar_tipe", key.TipeKamarId, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TipeKamarModel GetData(ITipeKamarKey key)
    {
        var sql = $@"{SelectFromClause()} 
            WHERE fs_kd_kamar_tipe = @fs_kd_kamar_tipe; ";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar_tipe", key.TipeKamarId, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TipeKamarDto>(sql, dp);
    }

    public IEnumerable<TipeKamarModel> ListData()
    {
        var sql = SelectFromClause();
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TipeKamarDto>(sql);
    }
    
    private static string SelectFromClause() =>
        @"
        SELECT 
            fs_kd_kamar_tipe,
            fs_nm_kamar_tipe,
            fb_gabung,
            fb_aktif,
            fb_default_tipe,
            fb_no_urut
        FROM ta_kamar_tipe
    "; 
}

public class TipeKamarDto() : TipeKamarModel(string.Empty,string.Empty)
{
    public string fs_kd_kamar_tipe{get => TipeKamarId; set => TipeKamarId = value; }
    public string fs_nm_kamar_tipe{get => TipeKamarName; set => TipeKamarName = value; }
    public bool  fb_gabung { get => IsGabung; set => IsGabung = value; }
    public bool  fb_aktif { get => IsAktif; set => IsAktif = value; }
    public bool  fb_default_tipe { get => IsDefault; set => IsDefault = value; }
    public int fb_no_urut { get => NoUrut; set => NoUrut = value; }
}
