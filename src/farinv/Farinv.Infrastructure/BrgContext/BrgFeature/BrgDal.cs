using System.Data;
using System.Data.SqlClient;
using Dapper;
using Farinv.Application.BrgContext.BrgFeature;
using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Infrastructure.BrgContext.BrgFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

public interface IBrgDal :
    IInsert<BrgDto>,
    IUpdate<BrgDto>,
    IDelete<IBrgKey>,
    IGetData<BrgDto, IBrgKey>,
    IListData<BrgView, string>
{
}

public class BrgDal : IBrgDal
{
    private readonly DatabaseOptions _opt;
    public BrgDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(BrgDto dto)
    {
        const string sql = """
            INSERT INTO tb_barang(
                fs_kd_barang, fs_nm_barang, fb_aktif,
                fs_ket_barang, fs_kd_grup_rek,
                fs_kd_golongan, fs_kd_kelompok, fs_kd_sifat,
                fs_kd_bentuk, fs_kd_pabrik, fs_kd_gol_obat_dk,
                fs_kd_generik, fs_kd_gol_terapi, fs_kd_kelas_terapi,
                fs_kd_original)
            VALUES( 
                @fs_kd_barang, @fs_nm_barang, @fb_aktif,
                @fs_ket_barang, @fs_kd_grup_rek,
                @fs_kd_golongan, @fs_kd_kelompok, @fs_kd_sifat,
                @fs_kd_bentuk, @fs_kd_pabrik, @fs_kd_gol_obat_dk,
                @fs_kd_generik, @fs_kd_gol_terapi, @fs_kd_kelas_terapi,
                @fs_kd_original)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", dto.fs_kd_barang, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_barang", dto.fs_nm_barang, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fs_ket_barang", dto.fs_ket_barang, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_grup_rek", dto.fs_kd_grup_rek, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_golongan", dto.fs_kd_golongan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelompok", dto.fs_kd_kelompok, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_sifat", dto.fs_kd_sifat, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_bentuk", dto.fs_kd_bentuk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pabrik", dto.fs_kd_pabrik, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_gol_obat_dk", dto.fs_kd_gol_obat_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_generik", dto.fs_kd_generik, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_gol_terapi", dto.fs_kd_gol_terapi, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelas_terapi", dto.fs_kd_kelas_terapi, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_original", dto.fs_kd_original, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Update(BrgDto dto)
    {
        const string sql = """
           UPDATE 
               tb_barang
           SET
               fs_nm_barang = @fs_nm_barang,
               fb_aktif = @fb_aktif,
               fs_ket_barang = @fs_ket_barang,
               fs_kd_grup_rek = @fs_kd_grup_rek,
               fs_kd_golongan = @fs_kd_golongan,
               fs_kd_kelompok = @fs_kd_kelompok,
               fs_kd_sifat = @fs_kd_sifat,
               fs_kd_bentuk = @fs_kd_bentuk,
               fs_kd_pabrik = @fs_kd_pabrik,
               fs_kd_gol_obat_dk = @fs_kd_gol_obat_dk,
               fs_kd_generik = @fs_kd_generik,
               fs_kd_gol_terapi = @fs_kd_gol_terapi,
               fs_kd_kelas_terapi = @fs_kd_kelas_terapi,
               fs_kd_original = @fs_kd_original
           WHERE
               fs_kd_barang = @fs_kd_barang
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", dto.fs_kd_barang, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_barang", dto.fs_nm_barang, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fs_ket_barang", dto.fs_ket_barang, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_grup_rek", dto.fs_kd_grup_rek, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_golongan", dto.fs_kd_golongan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelompok", dto.fs_kd_kelompok, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_sifat", dto.fs_kd_sifat, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_bentuk", dto.fs_kd_bentuk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pabrik", dto.fs_kd_pabrik, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_gol_obat_dk", dto.fs_kd_gol_obat_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_generik", dto.fs_kd_generik, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_gol_terapi", dto.fs_kd_gol_terapi, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelas_terapi", dto.fs_kd_kelas_terapi, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_original", dto.fs_kd_original, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Delete(IBrgKey key)
    {
        const string sql = """
           DELETE FROM 
                tb_barang
           WHERE
               fs_kd_barang = @fs_kd_barang
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", key.BrgId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public BrgDto GetData(IBrgKey key)
    {
        const string sql = """
           SELECT
               aa.fs_kd_barang,
               aa.fs_nm_barang,
               aa.fb_aktif,
               aa.fs_ket_barang,
               aa.fs_kd_grup_rek,
               aa.fs_kd_golongan,
               aa.fs_kd_kelompok,
               aa.fs_kd_sifat,
               aa.fs_kd_bentuk,
               aa.fs_kd_pabrik,
               aa.fs_kd_gol_obat_dk,
               aa.fs_kd_generik,
               aa.fs_kd_gol_terapi,
               aa.fs_kd_kelas_terapi,
               aa.fs_kd_original,
               ISNULL(bb.fs_nm_grup_rek, '') fs_nm_grup_rek,
               ISNULL(cc.fs_kd_grup_rek_dk, '') fs_kd_grup_rek_dk,
               ISNULL(cc.fs_nm_grup_rek_dk, '') fs_nm_grup_rek_dk,
               ISNULL(dd.fs_nm_golongan, '') fs_nm_golongan,
               ISNULL(ee.fs_nm_kelompok, '') fs_nm_kelompok,
               ISNULL(ff.fs_nm_sifat, '') fs_nm_sifat,
               ISNULL(gg.fs_nm_bentuk, '') fs_nm_bentuk,
               ISNULL(hh.fs_nm_pabrik, '') fs_nm_pabrik,
               ISNULL(ii.fs_nm_gol_obat_dk, '') fs_nm_gol_obat_dk,
               ISNULL(jj.fs_nm_generik, '') fs_nm_generik,
               ISNULL(kk.fs_nm_gol_terapi, '') fs_nm_gol_terapi,
               ISNULL(ll.fs_nm_kelas_terapi, '') fs_nm_kelas_terapi,
               ISNULL(mm.fs_nm_original, '') fs_nm_original
           FROM 
               tb_barang aa
               LEFT JOIN tb_grup_rek bb ON aa.fs_kd_grup_rek = bb.fs_kd_grup_rek
               LEFT JOIN tb_grup_rek_dk cc ON bb.fs_kd_grup_rek_dk = cc.fs_kd_grup_rek_dk
               LEFT JOIN tb_golongan dd ON aa.fs_kd_golongan = dd.fs_kd_golongan
               LEFT JOIN tb_kelompok ee ON aa.fs_kd_kelompok = ee.fs_kd_kelompok
               LEFT JOIN tb_sifat ff ON aa.fs_kd_sifat = ff.fs_kd_sifat
               LEFT JOIN tb_bentuk gg ON aa.fs_kd_bentuk = gg.fs_kd_bentuk
               LEFT JOIN tb_pabrik hh ON aa.fs_kd_pabrik = hh.fs_kd_pabrik
               LEFT JOIN tb_gol_obat_dk ii ON aa.fs_kd_gol_obat_dk = ii.fs_kd_gol_obat_dk
               LEFT JOIN tb_generik jj ON aa.fs_kd_generik = jj.fs_kd_generik
               LEFT JOIN tb_gol_terapi kk ON aa.fs_kd_gol_terapi = kk.fs_kd_gol_terapi
               LEFT JOIN tb_kelas_terapi ll ON aa.fs_kd_kelas_terapi = ll.fs_kd_kelas_terapi
               LEFT JOIN tb_original mm ON aa.fs_kd_original = mm.fs_kd_original
           WHERE
               aa.fs_kd_barang = @fs_kd_barang
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", key.BrgId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<BrgDto>(sql, dp);
        return result;
    }
    
    public IEnumerable<BrgView> ListData(string keyword)
    {
        const string sql = """
            SELECT
                aa.fs_kd_barang AS BrgId,
                aa.fs_nm_barang AS BrgName,
                aa.fs_ket_barang AS KetBarang,
                ISNULL(cc.fs_kd_grup_rek_dk, '') AS GroupRekDkId
            FROM 
                tb_barang aa
                LEFT JOIN tb_grup_rek bb ON aa.fs_kd_grup_rek = bb.fs_kd_grup_rek
                LEFT JOIN tb_grup_rek_dk cc ON bb.fs_kd_grup_rek_dk = cc.fs_kd_grup_rek_dk
            WHERE
                CONTAINS(fs_nm_barang, @keyword)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@Keyword", keyword, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BrgView>(sql,dp);
    }
}