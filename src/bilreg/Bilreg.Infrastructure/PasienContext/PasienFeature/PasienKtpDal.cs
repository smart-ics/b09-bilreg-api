using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public interface IPasienKtpDal : 
    IInsert<PasienKtpDto>,
    IUpdate<PasienKtpDto>,
    IDelete<IPasienKey>,
    IGetData<PasienKtpDto, IPasienKey>{}

public class PasienKtpDal : IPasienKtpDal
{
    private readonly DatabaseOptions _opt;

    public PasienKtpDal(IOptions<DatabaseOptions>  opt)
    {
        _opt = opt.Value;
    }
    public void Insert(PasienKtpDto dto)
    {
        const string sql = """
            INSERT INTO tc_mr_ktp(
               fs_kd_mr, fs_nik, fs_nama_ktp, fs_alm_ktp, fs_rt_ktp, fs_rw_ktp,  
               fs_kd_kelurahan_ktp, fs_kelurahan_ktp, fs_kd_kecamatan_ktp, fs_kecamatan_ktp, 
               fs_kd_kabupaten_ktp, fs_kabupaten_ktp, fs_kd_propinsi_ktp, fs_propinsi_ktp, 
               fs_tempat_lahir, fs_sex, fd_tgl_lahir, fs_gol_darah)
            VALUES(
               @fs_kd_mr, @fs_nik, @fs_nama_ktp, @fs_alm_ktp, @fs_rt_ktp, @fs_rw_ktp,  
               @fs_kd_kelurahan_ktp, @fs_kelurahan_ktp, @fs_kd_kecamatan_ktp, @fs_kecamatan_ktp, 
               @fs_kd_kabupaten_ktp, @fs_kabupaten_ktp, @fs_kd_propinsi_ktp, @fs_propinsi_ktp, 
               @fs_tempat_lahir, @fs_sex, @fd_tgl_lahir, @fs_gol_darah)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_mr", dto.fs_kd_mr, SqlDbType.VarChar); 
        dp.AddParam("@fs_nik", dto.fs_nik, SqlDbType.VarChar); 
        dp.AddParam("@fs_nama_ktp", dto.fs_nama_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_alm_ktp", dto.fs_alm_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_rt_ktp", dto.fs_rt_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_rw_ktp", dto.fs_rw_ktp, SqlDbType.VarChar);  
        dp.AddParam("@fs_kd_kelurahan_ktp", dto.fs_kd_kelurahan_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_kelurahan_ktp", dto.fs_kelurahan_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_kecamatan_ktp", dto.fs_kd_kecamatan_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_kecamatan_ktp", dto.fs_kecamatan_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_kabupaten_ktp", dto.fs_kd_kabupaten_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_kabupaten_ktp", dto.fs_kabupaten_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_propinsi_ktp", dto.fs_kd_propinsi_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_propinsi_ktp", dto.fs_propinsi_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_tempat_lahir", dto.fs_tempat_lahir, SqlDbType.VarChar); 
        dp.AddParam("@fs_sex", dto.fs_sex, SqlDbType.VarChar); 
        dp.AddParam("@fd_tgl_lahir", dto.fd_tgl_lahir, SqlDbType.VarChar); 
        dp.AddParam("@fs_gol_darah", dto.fs_gol_darah, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PasienKtpDto dto)
    {
        const string sql = """
            UPDATE 
                tc_mr_ktp
            SET
               fs_nik = @fs_nik, 
               fs_nama_ktp = @fs_nama_ktp, 
               fs_alm_ktp = @fs_alm_ktp, 
               fs_rt_ktp = @fs_rt_ktp, 
               fs_rw_ktp = @fs_rw_ktp,  
               fs_kd_kelurahan_ktp = @fs_kd_kelurahan_ktp, 
               fs_kelurahan_ktp = @fs_kelurahan_ktp, 
               fs_kd_kecamatan_ktp = @fs_kd_kecamatan_ktp, 
               fs_kecamatan_ktp = @fs_kecamatan_ktp, 
               fs_kd_kabupaten_ktp = @fs_kd_kabupaten_ktp, 
               fs_kabupaten_ktp = @fs_kabupaten_ktp, 
               fs_kd_propinsi_ktp = @fs_kd_propinsi_ktp, 
               fs_propinsi_ktp = @fs_propinsi_ktp, 
               fs_tempat_lahir = @fs_tempat_lahir, 
               fs_sex = @fs_sex, 
               fd_tgl_lahir = @fd_tgl_lahir, 
               fs_gol_darah = @fs_gol_darah
            WHERE
               fs_kd_mr = @fs_kd_mr
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_mr", dto.fs_kd_mr, SqlDbType.VarChar); 
        dp.AddParam("@fs_nik", dto.fs_nik, SqlDbType.VarChar); 
        dp.AddParam("@fs_nama_ktp", dto.fs_nama_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_alm_ktp", dto.fs_alm_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_rt_ktp", dto.fs_rt_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_rw_ktp", dto.fs_rw_ktp, SqlDbType.VarChar);  
        dp.AddParam("@fs_kd_kelurahan_ktp", dto.fs_kd_kelurahan_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_kelurahan_ktp", dto.fs_kelurahan_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_kecamatan_ktp", dto.fs_kd_kecamatan_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_kecamatan_ktp", dto.fs_kecamatan_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_kabupaten_ktp", dto.fs_kd_kabupaten_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_kabupaten_ktp", dto.fs_kabupaten_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_propinsi_ktp", dto.fs_kd_propinsi_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_propinsi_ktp", dto.fs_propinsi_ktp, SqlDbType.VarChar); 
        dp.AddParam("@fs_tempat_lahir", dto.fs_tempat_lahir, SqlDbType.VarChar); 
        dp.AddParam("@fs_sex", dto.fs_sex, SqlDbType.VarChar); 
        dp.AddParam("@fd_tgl_lahir", dto.fd_tgl_lahir, SqlDbType.VarChar); 
        dp.AddParam("@fs_gol_darah", dto.fs_gol_darah, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPasienKey key)
    {
        const string sql = """
            DELETE FROM 
                tc_mr_ktp
            WHERE
               fs_kd_mr = @fs_kd_mr
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_mr", key.PasienId, SqlDbType.VarChar); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PasienKtpDto GetData(IPasienKey key)
    {
        const string sql = """
            SELECT 
                fs_kd_mr, fs_nik, fs_nama_ktp, fs_alm_ktp, fs_rt_ktp, fs_rw_ktp,  
                fs_kd_kelurahan_ktp, fs_kelurahan_ktp, fs_kd_kecamatan_ktp, fs_kecamatan_ktp, 
                fs_kd_kabupaten_ktp, fs_kabupaten_ktp, fs_kd_propinsi_ktp, fs_propinsi_ktp, 
                fs_tempat_lahir, fs_sex, fd_tgl_lahir, fs_gol_darah
            FROM
                tc_mr_ktp
            WHERE
               fs_kd_mr = @fs_kd_mr
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_mr", key.PasienId, SqlDbType.VarChar); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PasienKtpDto>(sql, dp);
    }
}
