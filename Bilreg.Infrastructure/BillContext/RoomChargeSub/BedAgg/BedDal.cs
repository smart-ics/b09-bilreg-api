using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BillContext.RoomChargeSub.BedAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.BedAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KamarAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BillContext.RoomChargeSub.BedAgg;

public class BedDal : IBedDal
{
    private readonly DatabaseOptions _opt;

    public BedDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(BedModel model)
    {
        const string sql = @"
            INSERT INTO ta_bed
                (fs_kd_bed, fs_nm_bed, fs_keterangan, fb_aktif, fs_kd_kamar)
            VALUES 
                (@fs_kd_bed, @fs_nm_bed, @fs_keterangan, @fb_aktif, @fs_kd_kamar)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bed", model.BedId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_bed", model.BedName, SqlDbType.VarChar);
        dp.AddParam("@fs_keterangan", model.Keterangan, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fs_kd_kamar", model.KamarId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(BedModel model)
    {
        const string sql = @"
            UPDATE 
                ta_bed
            SET
                fs_nm_bed = @fs_nm_bed,
                fs_keterangan = @fs_keterangan,
                fb_aktif = @fb_aktif, 
                fs_kd_kamar = @fs_kd_kamar
            WHERE 
                fs_kd_bed = @fs_kd_bed";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bed", model.BedId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_bed", model.BedName, SqlDbType.VarChar);
        dp.AddParam("@fs_keterangan", model.Keterangan, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fs_kd_kamar", model.KamarId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IBedKey key)
    {
        const string sql = @"
            DELETE FROM 
                ta_bed
            WHERE 
                fs_kd_bed = @fs_kd_bed";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bed", key.BedId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public BedModel GetData(IBedKey key)
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_bed, 
                aa.fs_nm_bed, 
                aa.fs_keterangan, 
                aa.fb_aktif, 
                aa.fs_kd_kamar,
                ISNULL(bb.fs_nm_kamar, '') AS fs_nm_kamar,
                ISNULL(cc.fs_kd_bangsal, '') AS fs_kd_bangsal,
                ISNULL(cc.fs_nm_bangsal, '') AS fs_nm_bangsal
            FROM 
                ta_bed aa
                LEFT JOIN ta_kamar bb ON aa.fs_kd_kamar = bb.fs_kd_kamar
                LEFT JOIN ta_bangsal cc ON bb.fs_kd_bangsal = cc.fs_kd_bangsal
            WHERE 
                aa.fs_kd_bed = @fs_kd_bed";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bed", key.BedId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<BedDto>(sql, dp);
        return result;
    }

    public IEnumerable<BedModel> ListData(IBangsalKey filter)
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_bed, 
                aa.fs_nm_bed, 
                aa.fs_keterangan, 
                aa.fb_aktif, 
                aa.fs_kd_kamar,
                ISNULL(bb.fs_nm_kamar, '') AS fs_nm_kamar,
                ISNULL(cc.fs_kd_bangsal, '') AS fs_kd_bangsal,
                ISNULL(cc.fs_nm_bangsal, '') AS fs_nm_bangsal
            FROM 
                ta_bed aa
                LEFT JOIN ta_kamar bb ON aa.fs_kd_kamar = bb.fs_kd_kamar
                LEFT JOIN ta_bangsal cc ON bb.fs_kd_bangsal = cc.fs_kd_bangsal
            WHERE
                ISNULL(cc.fs_kd_bangsal, '') = @fs_kd_bangsal";


        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bangsal", filter.BangsalId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BedDto>(sql,dp);
        
    }
}

public class BedDto() : BedModel("","")
{
    public void SetTestDataBed()
    {
        BedId = "A";
        BedName = "B";
        Keterangan = "C";
        IsAktif = false;
        KamarId = "D";
        KamarName = "";
        BangsalId = "";
        BangsalName = "";
    }
    
    public string fs_kd_bed { get => BedId ; set => BedId = value; }
    public string fs_nm_bed { get => BedName; set => BedName = value; }
    public string fs_keterangan { get => Keterangan; set => Keterangan = value; }
    public bool fb_aktif { get => IsAktif; set => IsAktif = value; }
    
    public string fs_kd_kamar { get => KamarId; set => KamarId = value; }
    public string fs_nm_kamar { get => KamarName; set => KamarName = value; }
    
    public string fs_kd_bangsal { get => BangsalId; set => BangsalId = value; }
    public string fs_nm_bangsal { get => BangsalName; set => BangsalName = value; }
}