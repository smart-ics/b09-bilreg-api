using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public interface IKarcisDal :
    IInsert<KarcisDto>,
    IUpdate<KarcisDto>,
    IDelete<IKarcisKey>,
    IGetData<KarcisDto, IKarcisKey>,
    IListData<KarcisDto, IInstalasiDkKey>
{
}

public class KarcisDal: IKarcisDal
{
    private readonly DatabaseOptions _opt;

    public KarcisDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KarcisDto dto)
    {
        const string sql = """
            INSERT INTO ta_karcis(
                fs_kd_karcis, fs_nm_karcis, fn_karcis, fs_kd_instalasi_dk, 
                fs_kd_rekap_cetak, fs_kd_tarif, fb_aktif)
            VALUES(
                @fs_kd_karcis, @fs_nm_karcis, @fn_karcis, @fs_kd_instalasi_dk, 
                @fs_kd_rekap_cetak, @fs_kd_tarif, @fb_aktif)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_karcis", dto.fs_kd_karcis, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_karcis", dto.fs_nm_karcis, SqlDbType.VarChar);
        dp.AddParam("@fn_karcis", dto.fn_karcis, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_instalasi_dk", dto.fs_kd_instalasi_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rekap_cetak", dto.fs_kd_rekap_cetak, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tarif", dto.fs_kd_tarif, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KarcisDto dto)
    {
        const string sql = """
            UPDATE ta_karcis
            SET 
                fs_nm_karcis = @fs_nm_karcis,
                fn_karcis = @fn_karcis,
                fs_kd_instalasi_dk = @fs_kd_instalasi_dk,
                fs_kd_rekap_cetak = @fs_kd_rekap_cetak,
                fs_kd_tarif = @fs_kd_tarif,
                fb_aktif = @fb_aktif
            WHERE
                fs_kd_karcis = @fs_kd_karcis
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_karcis", dto.fs_kd_karcis, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_karcis", dto.fs_nm_karcis, SqlDbType.VarChar);
        dp.AddParam("@fn_karcis", dto.fn_karcis, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_instalasi_dk", dto.fs_kd_instalasi_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rekap_cetak", dto.fs_kd_rekap_cetak, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tarif", dto.fs_kd_tarif, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    public void Delete(IKarcisKey key)
    {
        const string sql = """
            DELETE FROM
                ta_karcis
            WHERE
               fs_kd_karcis = @fs_kd_karcis
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_karcis", key.KarcisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public KarcisDto GetData(IKarcisKey key)
    {
        const string sql = """
           SELECT
               aa.fs_kd_karcis, aa.fs_nm_karcis, aa.fn_karcis, aa.fs_kd_instalasi_dk,
               aa.fs_kd_rekap_cetak, aa.fs_kd_tarif, aa.fb_aktif,
               ISNULL(bb.fs_nm_instalasi_dk, '') AS fs_nm_instalasi_dk,
               ISNULL(cc.fs_nm_rekap_cetak_tarif, '') AS fs_nm_rekap_cetak,
               ISNULL(dd.fs_nm_tarif, '') AS fs_nm_tarif
           FROM ta_karcis aa
               LEFT JOIN ta_instalasi_dk bb ON aa.fs_kd_instalasi_dk = bb.fs_kd_instalasi_dk
               LEFT JOIN ta_rekap_cetak_tarif cc ON aa.fs_kd_rekap_cetak = cc.fs_kd_rekap_cetak_tarif
               LEFT JOIN ta_tarif dd ON aa.fs_kd_tarif = dd.fs_kd_tarif        
           WHERE 
               aa.fs_kd_karcis = @fs_kd_karcis
           """;
        
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_karcis", key.KarcisId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<KarcisDto>(sql, dp);
    }

    public IEnumerable<KarcisDto> ListData(IInstalasiDkKey filter)
    {
        const string sql = """
            SELECT
                aa.fs_kd_karcis, aa.fs_nm_karcis, aa.fn_karcis, aa.fs_kd_instalasi_dk,
                aa.fs_kd_rekap_cetak, aa.fs_kd_tarif, aa.fb_aktif,
                ISNULL(bb.fs_nm_instalasi_dk, '') AS fs_nm_instalasi_dk,
                ISNULL(cc.fs_nm_rekap_cetak_tarif, '') AS fs_nm_rekap_cetak,
                ISNULL(dd.fs_nm_tarif, '') AS fs_nm_tarif
            FROM ta_karcis aa
                LEFT JOIN ta_instalasi_dk bb ON aa.fs_kd_instalasi_dk = bb.fs_kd_instalasi_dk
                LEFT JOIN ta_rekap_cetak_tarif cc ON aa.fs_kd_rekap_cetak = cc.fs_kd_rekap_cetak_tarif
                LEFT JOIN ta_tarif dd ON aa.fs_kd_tarif = dd.fs_kd_tarif
            WHERE
                aa.fs_kd_instalasi_dk = @fs_kd_instalasi_dk
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_instalasi_dk", filter.InstalasiDkId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KarcisDto>(sql, dp);
    }
}
