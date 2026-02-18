using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;
using System.Data;
using System.Data.SqlClient;

//  resharper disable inconsistentnaming
namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public interface IRegDal :
    IInsert<RegDto>,
    IUpdate<RegDto>,
    IDelete<IRegKey>,
    IGetData<RegDto, IRegKey>,
    IGetData<RegDto, IBookingKey>,
    IListData<RegDto, Periode, ILayananKey>,
    IListData<RegDto, IPasienKey>,
    IListData<RegDto, DateTime>
{
    IEnumerable<RegDto> ListDataByName(Dictionary<string, string[]> listName);
}

public class RegDal : IRegDal
{
    private readonly DatabaseOptions _opt;

    public RegDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(RegDto dto)
    {
        const string sql = """
            INSERT INTO ta_registrasi (
                fs_kd_reg, fd_tgl_masuk, fs_jam_masuk, fs_kd_petugas,
                fd_tgl_keluar, fs_jam_keluar, fs_kd_petugas_keluar,
                fd_tgl_cancel_out, fs_jam_cancel_out, fs_kd_petugas_cancel_out,
                fs_kd_jenis_reg, fs_mr, fs_kd_tipe_jaminan, fs_kd_kelas, 
                fs_kd_cara_masuk_dk, fs_kd_rujukan, fs_kd_medis, 
                fs_kd_layanan, fs_kd_karcis)
            VALUES (
                @fs_kd_reg, @fd_tgl_masuk,  @fs_jam_masuk, @fs_kd_petugas,
                @fd_tgl_keluar, @fs_jam_keluar, @fs_kd_petugas_keluar,
                @fd_tgl_cancel_out, @fs_jam_cancel_out, @fs_kd_petugas_cancel_out,
                @fs_kd_jenis_reg, @fs_mr, @fs_kd_tipe_jaminan, @fs_kd_kelas, 
                @fs_kd_cara_masuk_dk, @fs_kd_rujukan, @fs_kd_medis, 
                @fs_kd_layanan, @fs_kd_karcis)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", dto.fs_kd_reg, SqlDbType.VarChar); 
        dp.AddParam("@fd_tgl_masuk", dto.fd_tgl_masuk, SqlDbType.VarChar);  
        dp.AddParam("@fs_jam_masuk", dto.fs_jam_masuk, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_petugas", dto.fs_kd_petugas, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_keluar", dto.fd_tgl_keluar, SqlDbType.VarChar); 
        dp.AddParam("@fs_jam_keluar", dto.fs_jam_keluar, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_petugas_keluar", dto.fs_kd_petugas_keluar, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_cancel_out", dto.fd_tgl_cancel_out, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_cancel_out", dto.fs_jam_cancel_out, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_petugas_cancel_out", dto.fs_kd_petugas_cancel_out, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_jenis_reg", dto.fs_kd_jenis_reg, SqlDbType.VarChar);
        dp.AddParam("@fs_mr", dto.fs_mr, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_jaminan", dto.fs_kd_tipe_jaminan, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_kelas", dto.fs_kd_kelas, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_cara_masuk_dk", dto.fs_kd_cara_masuk_dk, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_rujukan", dto.fs_kd_rujukan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_medis", dto.fs_kd_medis, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_karcis", dto.fs_kd_karcis, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(RegDto dto)
    {
        const string sql = """
            UPDATE
                ta_registrasi
            SET
                fd_tgl_masuk = @fd_tgl_masuk,  
                fs_jam_masuk = @fs_jam_masuk, 
                fs_kd_petugas = @fs_kd_petugas,
                fd_tgl_keluar = @fd_tgl_keluar, 
                fs_jam_keluar = @fs_jam_keluar, 
                fs_kd_petugas_keluar = @fs_kd_petugas_keluar,
                fd_tgl_cancel_out = @fd_tgl_cancel_out, 
                fs_jam_cancel_out = @fs_jam_cancel_out, 
                fs_kd_petugas_cancel_out = @fs_kd_petugas_cancel_out,
                fs_kd_jenis_reg = @fs_kd_jenis_reg, 
                fs_mr = @fs_mr, 
                fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan, 
                fs_kd_kelas = @fs_kd_kelas, 
                fs_kd_cara_masuk_dk = @fs_kd_cara_masuk_dk, 
                fs_kd_rujukan = @fs_kd_rujukan, 
                fs_kd_medis = @fs_kd_medis, 
                fs_kd_layanan = @fs_kd_layanan, 
                fs_kd_karcis = @fs_kd_karcis
            WHERE
                fs_kd_reg = @fs_kd_reg
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", dto.fs_kd_reg, SqlDbType.VarChar); 
        dp.AddParam("@fd_tgl_masuk", dto.fd_tgl_masuk, SqlDbType.VarChar);  
        dp.AddParam("@fs_jam_masuk", dto.fs_jam_masuk, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_petugas", dto.fs_kd_petugas, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_keluar", dto.fd_tgl_keluar, SqlDbType.VarChar); 
        dp.AddParam("@fs_jam_keluar", dto.fs_jam_keluar, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_petugas_keluar", dto.fs_kd_petugas_keluar, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_cancel_out", dto.fd_tgl_cancel_out, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_cancel_out", dto.fs_jam_cancel_out, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_petugas_cancel_out", dto.fs_kd_petugas_cancel_out, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_jenis_reg", dto.fs_kd_jenis_reg, SqlDbType.VarChar);
        dp.AddParam("@fs_mr", dto.fs_mr, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_jaminan", dto.fs_kd_tipe_jaminan, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_kelas", dto.fs_kd_kelas, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_cara_masuk_dk", dto.fs_kd_cara_masuk_dk, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_rujukan", dto.fs_kd_rujukan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_medis", dto.fs_kd_medis, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar); 
        dp.AddParam("@fs_kd_karcis", dto.fs_kd_karcis, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IRegKey key)
    {
        const string sql = """
            DELETE FROM
                ta_registrasi
            WHERE
                fs_kd_reg = @fs_kd_reg
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", key.RegId, SqlDbType.VarChar); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public RegDto GetData(IRegKey key)
    {
        var sql = SelectFromClause() + @"
            WHERE
                aa.fs_kd_reg = @fs_kd_reg 
            ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", key.RegId, SqlDbType.VarChar); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<RegDto>(sql, dp);
    }

    public RegDto GetData(IBookingKey key)
    {
        var sql = SelectFromClause() + @"
            WHERE
                aa.fs_kd_booking = @fs_kd_booking
                AND aa.fd_tgl_void = '3000-01-01'
            ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_booking", key.BookingId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<RegDto>(sql, dp);
    }

    public IEnumerable<RegDto> ListData(Periode periode, ILayananKey layanan)
    {
        var sql = SelectFromClause() + @"
            WHERE
                aa.fd_tgl_masuk BETWEEN @tgl1 AND @tgl2
                AND aa.fs_kd_Layanan = @fs_kd_layanan
                AND aa.fd_tgl_void = '3000-01-01'
             ";

        var dp = new DynamicParameters();
        dp.AddParam("@tgl1", periode.Tgl1.ToString("yyyy-MM-dd"), SqlDbType.VarChar); 
        dp.AddParam("@tgl2", periode.Tgl2.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", layanan.LayananId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RegDto>(sql, dp);
    }

    public IEnumerable<RegDto> ListData(IPasienKey filter)
    {
        var sql = SelectFromClause() + @"
            WHERE
                aa.fs_mr = @fs_mr
                AND aa.fd_tgl_void = '3000-01-01'
             ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", filter.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RegDto>(sql, dp);
    }

    public IEnumerable<RegDto> ListData(DateTime filter)
    {
        var sql = SelectFromClause() + @"
            WHERE
                aa.fd_tgl_masuk = @tgl
                AND aa.fd_tgl_void = '3000-01-01'
             ";

        var dp = new DynamicParameters();
        dp.AddParam("@tgl", filter.Date.ToString(DateFormatEnum.YMD), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RegDto>(sql, dp);
    }

    private static string EscapeForContains(string term)
    {
        return "\"" + term.Replace("\"", "\"\"") + "*\"";
    }
    public IEnumerable<RegDto> ListDataByName(Dictionary<string, string[]> listName)
    {
        var containers = listName
            .Select(item =>
            {
                var listVariant = item.Value.Select(EscapeForContains);
                return $"CONTAINS(fs_nm_pasien, '{string.Join(" OR ", listVariant)}')";
            });

        var whereClause = string.Join(" AND ", containers);

        var sql = SelectFromClause() + @$"
            WHERE
                {whereClause}
                AND aa.fd_tgl_void = '3000-01-01'
             ";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var results = conn.Read<RegDto>(sql) ?? [];
        return results;
    }

    public static string SelectFromClause()
        => @"
        SELECT
            aa.fs_kd_reg, aa.fd_tgl_masuk, aa.fs_jam_masuk, aa.fs_kd_petugas,
            aa.fd_tgl_keluar, aa.fs_jam_keluar, aa.fs_kd_petugas_keluar, 
            aa.fd_tgl_cancel_out, aa.fs_jam_cancel_out, aa.fs_kd_petugas_cancel_out, 
            aa.fs_kd_jenis_reg, aa.fs_mr, aa.fs_kd_tipe_jaminan, aa.fs_kd_kelas, 
            aa.fs_kd_cara_masuk_dk, aa.fs_kd_rujukan, aa.fs_kd_medis, 
            aa.fs_kd_layanan, aa.fs_kd_karcis,
            ISNULL(bb.fs_nm_pasien, '-') AS fs_nm_pasien, 
            ISNULL(bb.fd_tgl_lahir, '-') AS fd_tgl_lahir, 
            ISNULL(bb.fs_jns_kelamin, '-') AS fs_jns_kelamin,
            ISNULL(cc.fs_nm_tipe_jaminan, '-') AS fs_nm_tipe_jaminan,
            ISNULL(ee.fs_nm_kelas, '-') AS fs_nm_kelas,
            ISNULL(ff.fs_nm_cara_masuk_dk, '-') AS fs_nm_cara_masuk_dk,
            ISNULL(gg.fs_nm_rujukan, '-') AS fs_nm_rujukan,
            ISNULL(hh.fs_nm_peg, '-') AS fs_nm_medis,
            ISNULL(ii.fs_nm_layanan, '-') AS fs_nm_layanan,
            ISNULL(jj.fs_nm_karcis, '-') AS fs_nm_karcis
        FROM 
            ta_registrasi aa
            LEFT JOIN tc_mr bb ON aa.fs_mr = bb.fs_mr
            LEFT JOIN ta_tipe_jaminan cc ON aa.fs_kd_tipe_jaminan = cc.fs_kd_tipe_jaminan
            LEFT JOIN ta_kelas ee ON aa.fs_kd_kelas = ee.fs_kd_kelas
            LEFT JOIN ta_cara_masuk_dk ff ON aa.fs_kd_cara_masuk_dk = ff.fs_kd_cara_masuk_dk
            LEFT JOIN ta_rujukan gg ON aa.fs_kd_rujukan = gg.fs_kd_rujukan
            LEFT JOIN td_peg hh ON aa.fs_kd_medis = hh.fs_kd_peg
            LEFT JOIN ta_layanan ii ON aa.fs_kd_layanan = ii.fs_kd_layanan
            LEFT JOIN ta_karcis jj ON aa.fs_kd_karcis = jj.fs_kd_karcis ";
}