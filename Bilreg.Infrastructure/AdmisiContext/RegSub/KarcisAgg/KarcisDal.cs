using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;
using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RegSub.KarcisAgg;

public class KarcisDal: IKarcisDal
{
    private readonly DatabaseOptions _opt;

    public KarcisDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KarcisModel model)
    {
        const string sql = @"
            INSERT INTO ta_karcis(fs_kd_karcis, fs_nm_karcis, fs_kd_instalasi_dk, fs_kd_rekap_cetak, fs_kd_tarif, fb_aktif)
            VALUES(@fs_kd_karcis, @fs_nm_karcis, @fs_kd_instalasi_dk, @fs_kd_rekap_cetak, @fs_kd_tarif, @fb_aktif)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_karcis", model.KarcisId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_karcis", model.KarcisName, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_instalasi_dk", model.InstalasiDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rekap_cetak", model.RekapCetakId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tarif", model.TarifId, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KarcisModel model)
    {
        const string sql = @"
            UPDATE ta_karcis
            SET 
                fs_nm_karcis = @fs_nm_karcis,
                fs_kd_instalasi_dk = @fs_kd_instalasi_dk,
                fs_kd_rekap_cetak = @fs_kd_rekap_cetak,
                fs_kd_tarif = @fs_kd_tarif,
                fb_aktif = @fb_aktif
            WHERE
                fs_kd_karcis = @fs_kd_karcis";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_karcis", model.KarcisId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_karcis", model.KarcisName, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_instalasi_dk", model.InstalasiDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rekap_cetak", model.RekapCetakId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tarif", model.TarifId, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public KarcisModel GetData(IKarcisKey key)
    {
        var sql = $@"{SelectFromClause()}
            WHERE aa.fs_kd_karcis = @fs_kd_karcis";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_karcis", key.KarcisId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<KarcisDto>(sql, dp);
    }

    public IEnumerable<KarcisModel> ListData()
    {
        var sql = $@"{SelectFromClause()}";
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KarcisDto>(sql);
    }

    private static string SelectFromClause()
    {
        return @"
            SELECT
                aa.fs_kd_karcis, aa.fs_nm_karcis, aa.fs_kd_instalasi_dk,
                aa.fs_kd_rekap_cetak, aa.fs_kd_tarif, aa.fb_aktif,
                ISNULL(bb.fs_nm_instalasi_dk, '') AS fs_nm_instalasi_dk,
                ISNULL(cc.fs_nm_rekap_cetak_tarif, '') AS fs_nm_rekap_cetak,
                ISNULL(dd.fs_nm_tarif, '') AS fs_nm_tarif
            FROM ta_karcis aa
                LEFT JOIN ta_instalasi_dk bb ON aa.fs_kd_instalasi_dk = bb.fs_kd_instalasi_dk
                LEFT JOIN ta_rekap_cetak_tarif cc ON aa.fs_kd_rekap_cetak = cc.fs_kd_rekap_cetak_tarif
                LEFT JOIN ta_tarif dd ON aa.fs_kd_tarif = dd.fs_kd_tarif";
    }
}

public class KarcisDto() : KarcisModel(string.Empty, string.Empty)
{
    public string fs_kd_karcis { get => KarcisId; set => KarcisId = value; } 
    public string fs_nm_karcis { get => KarcisName; set => KarcisName = value; }
    public string fs_kd_instalasi_dk { get => InstalasiDkId; set => InstalasiDkId = value; } 
    public string fs_nm_instalasi_dk { get => InstalasiDkName; set => InstalasiDkName = value; } 
    public string fs_kd_rekap_cetak { get => RekapCetakId; set => RekapCetakId = value; } 
    public string fs_nm_rekap_cetak { get => RekapCetakName; set => RekapCetakName = value; } 
    public string fs_kd_tarif { get => TarifId; set => TarifId = value; } 
    public string fs_nm_tarif { get => TarifName; set => TarifName = value; } 
    public bool fb_aktif { get => IsAktif; set => IsAktif = value; } 
}