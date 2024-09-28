using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BillContext.RoomChargeSub.KamarAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KamarAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BillContext.RoomChargeSub.KamarAgg;

public class KamarDal : IKamarDal
{
    private readonly DatabaseOptions _opt;

    public KamarDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KamarModel model)
    {
        const string sql = @"
            INSERT INTO ta_kamar(
                fs_kd_kamar,fs_nm_kamar,
                fs_ket1,fs_ket2,fs_ket3,
                fn_jumlah,fn_pakai,fn_kotor,
                fn_rusak,fs_kd_bangsal,fs_kd_kelas
                )
            VALUES(
                @fs_kd_kamar,@fs_nm_kamar,
                @fs_ket1,@fs_ket2,@fs_ket3,
                @fn_jumlah,@fn_pakai,@fn_kotor,
                @fn_rusak,@fs_kd_bangsal,@fs_kd_kelas
                )";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar",model.KamarId ,SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kamar",model.KamarName ,SqlDbType.VarChar);
        dp.AddParam("@fs_ket1",model.Ket1 ,SqlDbType.VarChar);
        dp.AddParam("@fs_ket2",model.Ket2 ,SqlDbType.VarChar);
        dp.AddParam("@fs_ket3",model.Ket3 ,SqlDbType.VarChar);
        dp.AddParam("@fn_jumlah",model.JumlahKamar ,SqlDbType.Decimal);
        dp.AddParam("@fn_pakai",model.JumlahKamarPakai ,SqlDbType.Decimal);
        dp.AddParam("@fn_kotor",model.JumlahKamarKotor ,SqlDbType.Decimal);
        dp.AddParam("@fn_rusak",model.JumlahKamarRusak ,SqlDbType.Decimal);
        dp.AddParam("@fs_kd_bangsal",model.BangsalId ,SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelas", model.KelasId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KamarModel model)
    {
        const string sql = @"
                UPDATE ta_kamar
                SET 
                     fs_kd_kamar= @fs_kd_kamar,
                     fs_nm_kamar= @fs_nm_kamar,
                     fs_ket1= @fs_ket1,
                     fs_ket2= @fs_ket2,
                     fs_ket3= @fs_ket3,
                     fn_jumlah= @fn_jumlah,
                     fn_pakai= @fn_pakai,
                     fn_kotor= @fn_kotor,
                     fn_rusak= @fn_rusak,
                     fs_kd_bangsal= @fs_kd_bangsal,
                     fs_kd_kelas= @fs_kd_kelas
                 WHERE 
                    fs_kd_kamar = @fs_kd_kamar";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar",model.KamarId ,SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kamar",model.KamarName ,SqlDbType.VarChar);
        dp.AddParam("@fs_ket1",model.Ket1 ,SqlDbType.VarChar);
        dp.AddParam("@fs_ket2",model.Ket2 ,SqlDbType.VarChar);
        dp.AddParam("@fs_ket3",model.Ket3 ,SqlDbType.VarChar);
        dp.AddParam("@fn_jumlah",model.JumlahKamar ,SqlDbType.Decimal);
        dp.AddParam("@fn_pakai",model.JumlahKamarPakai ,SqlDbType.Decimal);
        dp.AddParam("@fn_kotor",model.JumlahKamarKotor ,SqlDbType.Decimal);
        dp.AddParam("@fn_rusak",model.JumlahKamarRusak ,SqlDbType.Decimal);
        dp.AddParam("@fs_kd_bangsal",model.BangsalId ,SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelas",model.KelasId ,SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKamarKey key)
    {
        const string sql = @"
            DELETE FROM ta_kamar
            WHERE fs_kd_kamar = @fs_kd_kamar ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar", key.KamarId, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public KamarModel GetData(IKamarKey key)
    {
        var sql = $@"{SelectFromClause()} 
            WHERE aa.fs_kd_kamar = @fs_kd_kamar ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar", key.KamarId, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<KamarDto>(sql, dp);
    }

    public IEnumerable<KamarModel> ListData(IBangsalKey filter)
    {
        var sql = $@"{SelectFromClause()} 
            WHERE aa.fs_kd_bangsal = @fs_kd_bangsal ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bangsal", filter.BangsalId, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KamarDto>(sql, dp);
    }

    public IEnumerable<KamarModel> ListData(IKelasKey filter)
    {
        var sql = $@"{SelectFromClause()} 
            WHERE aa.fs_kd_kelas = @fs_kd_kelas ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelas", filter.KelasId, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KamarDto>(sql, dp);
    }
    private static string SelectFromClause() =>
        @"
        SELECT 
        aa.fs_kd_kamar,
        aa.fs_nm_kamar,
        aa.fs_ket1,
        aa.fs_ket2,
        aa.fs_ket3,
        aa.fn_jumlah,
        aa.fn_pakai,
        aa.fn_kotor,
        aa.fn_rusak,
        aa.fs_kd_bangsal,
        aa.fs_kd_kelas,
        ISNULL(bb.fs_nm_bangsal,'') AS fs_nm_bangsal,
        ISNULL(cc.fs_nm_kelas,'') AS fs_nm_kelas

        FROM ta_kamar aa
        LEFT JOIN ta_bangsal bb ON aa.fs_kd_bangsal = bb.fs_kd_bangsal
        LEFT JOIN ta_kelas cc ON aa.fs_kd_kelas = cc.fs_kd_kelas
    "; 

}

public class KamarDto() : KamarModel(string.Empty,string.Empty)
{
    public string fs_kd_kamar{get => KamarId ;set => KamarId =value;}
    public string fs_nm_kamar{get => KamarName;set => KamarName =value;}
    public string fs_ket1{get => Ket1;set => Ket1=value;}
    public string fs_ket2{get => Ket2;set => Ket2 =value;}
    public string fs_ket3{get => Ket3;set => Ket3=value;}
    public decimal fn_jumlah{get => JumlahKamar;set => JumlahKamar =value;}
    public decimal fn_pakai{get => JumlahKamarPakai;set =>JumlahKamarPakai =value;}
    public decimal fn_kotor{get => JumlahKamarKotor;set => JumlahKamarKotor=value;}
    public decimal fn_rusak{get => JumlahKamarRusak;set => JumlahKamarRusak=value;}
    public string fs_kd_bangsal{get => BangsalId;set => BangsalId=value;}
    public string fs_nm_bangsal{get => BangsalName;set => BangsalName=value;}
    public string fs_kd_kelas{get => KelasId;set => KelasId=value;}
    public string fs_nm_kelas{get => KelasName;set => KelasName=value;}
}