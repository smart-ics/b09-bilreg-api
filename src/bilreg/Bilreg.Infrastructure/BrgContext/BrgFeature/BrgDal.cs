using System.Data;
using System.Data.SqlClient;
using Dapper;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Infrastructure.BrgContext.BrgFeature;
using Bilreg.Infrastructure.Shared.Helpers;
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
                '', '', '',
                '', @fs_kd_pabrik, '',
                '', '', '',
                '')
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", dto.fs_kd_barang, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_barang", dto.fs_nm_barang, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fs_ket_barang", dto.fs_ket_barang, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_grup_rek", dto.fs_kd_grup_rek, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pabrik", dto.fs_kd_pabrik, SqlDbType.VarChar);
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
               fs_kd_golongan = '',
               fs_kd_kelompok = '',
               fs_kd_sifat = '',
               fs_kd_bentuk = '',
               fs_kd_pabrik = @fs_kd_pabrik,
               fs_kd_gol_obat_dk = '',
               fs_kd_generik = '',
               fs_kd_gol_terapi = '',
               fs_kd_kelas_terapi = '',
               fs_kd_original = ''
           WHERE
               fs_kd_barang = @fs_kd_barang
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", dto.fs_kd_barang, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_barang", dto.fs_nm_barang, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fs_ket_barang", dto.fs_ket_barang, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_grup_rek", dto.fs_kd_grup_rek, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pabrik", dto.fs_kd_pabrik, SqlDbType.VarChar);
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
               aa.fs_kd_pabrik,
               ISNULL(bb.fs_nm_grup_rek, '') AS fs_nm_grup_rek,
               ISNULL(cc.fs_kd_grup_rek_dk, '') AS fs_kd_grup_rek_dk,
               ISNULL(cc.fs_nm_grup_rek_dk, '') AS fs_nm_grup_rek_dk,
               ISNULL(hh.fs_nm_pabrik, '') AS fs_nm_pabrik
           FROM 
               tb_barang aa
               LEFT JOIN tb_grup_rek bb ON aa.fs_kd_grup_rek = bb.fs_kd_grup_rek
               LEFT JOIN tb_grup_rek_dk cc ON bb.fs_kd_grup_rek_dk = cc.fs_kd_grup_rek_dk
               LEFT JOIN tb_pabrik hh ON aa.fs_kd_pabrik = hh.fs_kd_pabrik
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
        var keywordFts = FullTextSearch.GenKeywordContain(keyword);

        const string sql = """
            SELECT
                aa.fs_kd_barang AS BrgId,
                aa.fs_nm_barang AS BrgName,
                aa.fs_ket_barang AS KetBarang,
                ISNULL(cc.fs_kd_grup_rek_dk, '') AS GroupRekDkId,
                aa.fb_aktif AS IsAktif
            FROM 
                tb_barang aa
                LEFT JOIN tb_grup_rek bb ON aa.fs_kd_grup_rek = bb.fs_kd_grup_rek
                LEFT JOIN tb_grup_rek_dk cc ON bb.fs_kd_grup_rek_dk = cc.fs_kd_grup_rek_dk
            WHERE
                CONTAINS(fs_nm_barang, @keyword)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@Keyword", keywordFts, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        FullTextSearch.CheckAvailability(conn);
        return conn.Read<BrgView>(sql, dp);
    }
}