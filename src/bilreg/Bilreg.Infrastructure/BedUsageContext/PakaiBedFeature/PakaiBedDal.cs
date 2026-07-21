using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;

public interface IPakaiBedDal :
    IInsert<PakaiBedDto>,
    IUpdate<PakaiBedDto>,
    IDelete<IPakaiBed>,
    IGetData<PakaiBedDto, IPakaiBed>,
    IListData<PakaiBedDto, IRegKey>
{
}

public class PakaiBedDal : IPakaiBedDal
{
    private const string SELECT_FROM = """
        SELECT
            aa.fs_kd_trs,
            aa.fd_tgl_in, aa.fs_jam_in, aa.fs_kd_petugas,
            aa.fd_tgl_out, aa.fs_jam_out, aa.fs_kd_petugas_out,
            aa.fs_kd_reg, aa.fs_kd_layanan, aa.fs_kd_layanan_dk,
            aa.fs_kd_kamar_tipe, aa.fs_kd_bed, aa.fn_tarif,
            aa.fd_tgl_void, aa.fs_jam_void, aa.fs_kd_petugas_void,
            aa.fd_tgl_entry, aa.fs_jam_entry, aa.fs_kd_kelas,
            ISNULL(bb.fs_mr, '') AS fs_mr,
            ISNULL(cc.fs_nm_pasien, '') AS fs_nm_pasien,
            ISNULL(dd.fs_nm_layanan, '') AS fs_nm_layanan,
            ISNULL(ee.fs_nm_bed, '') AS fs_nm_bed,
            ISNULL(ee.fb_aktif, 0) AS fb_bed_aktif,
            ISNULL(ff.fs_nm_kamar_tipe, '') AS fs_nm_kamar_tipe,
            ISNULL(ff.fb_aktif, 0) AS fb_kamar_tipe_aktif,
            ISNULL(gg.fs_nm_kelas, '') AS fs_nm_kelas
        FROM
            ta_trs_bed aa
            LEFT JOIN ta_registrasi bb ON aa.fs_kd_reg = bb.fs_kd_reg
            LEFT JOIN tc_mr cc ON bb.fs_mr = cc.fs_mr
            LEFT JOIN ta_layanan dd ON aa.fs_kd_layanan = dd.fs_kd_layanan
            LEFT JOIN ta_bed ee ON aa.fs_kd_bed = ee.fs_kd_bed
            LEFT JOIN ta_kamar_tipe ff ON aa.fs_kd_kamar_tipe = ff.fs_kd_kamar_tipe
            LEFT JOIN ta_kelas gg ON aa.fs_kd_kelas = gg.fs_kd_kelas
        """;

    private readonly DatabaseOptions _opt;

    public PakaiBedDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PakaiBedDto dto)
    {
        const string sql = """
            INSERT INTO ta_trs_bed(
                fs_kd_trs,
                fd_tgl_in, fs_jam_in, fs_kd_petugas,
                fd_tgl_out, fs_jam_out, fs_kd_petugas_out,
                fs_kd_reg, fs_kd_layanan, fs_kd_layanan_dk,
                fs_kd_kamar_tipe, fs_kd_bed, fn_tarif,
                fd_tgl_void, fs_jam_void, fs_kd_petugas_void,
                fd_tgl_entry, fs_jam_entry, fs_kd_kelas)
            VALUES(
                @fs_kd_trs,
                @fd_tgl_in, @fs_jam_in, @fs_kd_petugas,
                @fd_tgl_out, @fs_jam_out, @fs_kd_petugas_out,
                @fs_kd_reg, @fs_kd_layanan, @fs_kd_layanan_dk,
                @fs_kd_kamar_tipe, @fs_kd_bed, @fn_tarif,
                @fd_tgl_void, @fs_jam_void, @fs_kd_petugas_void,
                @fd_tgl_entry, @fs_jam_entry, @fs_kd_kelas)
            """;

        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PakaiBedDto dto)
    {
        const string sql = """
            UPDATE
                ta_trs_bed
            SET
                fd_tgl_in = @fd_tgl_in,
                fs_jam_in = @fs_jam_in,
                fs_kd_petugas = @fs_kd_petugas,
                fd_tgl_out = @fd_tgl_out,
                fs_jam_out = @fs_jam_out,
                fs_kd_petugas_out = @fs_kd_petugas_out,
                fs_kd_reg = @fs_kd_reg,
                fs_kd_layanan = @fs_kd_layanan,
                fs_kd_layanan_dk = @fs_kd_layanan_dk,
                fs_kd_kamar_tipe = @fs_kd_kamar_tipe,
                fs_kd_bed = @fs_kd_bed,
                fn_tarif = @fn_tarif,
                fd_tgl_void = @fd_tgl_void,
                fs_jam_void = @fs_jam_void,
                fs_kd_petugas_void = @fs_kd_petugas_void,
                fd_tgl_entry = @fd_tgl_entry,
                fs_jam_entry = @fs_jam_entry,
                fs_kd_kelas = @fs_kd_kelas
            WHERE
                fs_kd_trs = @fs_kd_trs
            """;

        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPakaiBed key)
    {
        const string sql = """
            DELETE FROM
                ta_trs_bed
            WHERE
                fs_kd_trs = @fs_kd_trs
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.PakaiBedId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PakaiBedDto GetData(IPakaiBed key)
    {
        var sql = $"{SELECT_FROM}\nWHERE aa.fs_kd_trs = @fs_kd_trs";
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.PakaiBedId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PakaiBedDto>(sql, dp);
    }

    public IEnumerable<PakaiBedDto> ListData(IRegKey filter)
    {
        var sql = $"{SELECT_FROM}\nWHERE aa.fs_kd_reg = @fs_kd_reg";
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", filter.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PakaiBedDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(PakaiBedDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", dto.fs_kd_trs, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_in", dto.fd_tgl_in, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_in", dto.fs_jam_in, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas", dto.fs_kd_petugas, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_out", dto.fd_tgl_out, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_out", dto.fs_jam_out, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_out", dto.fs_kd_petugas_out, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_reg", dto.fs_kd_reg, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan_dk", dto.fs_kd_layanan_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kamar_tipe", dto.fs_kd_kamar_tipe, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_bed", dto.fs_kd_bed, SqlDbType.VarChar);
        dp.AddParam("@fn_tarif", dto.fn_tarif, SqlDbType.Decimal);
        dp.AddParam("@fd_tgl_void", dto.fd_tgl_void, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_void", dto.fs_jam_void, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_void", dto.fs_kd_petugas_void, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_entry", dto.fd_tgl_entry, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_entry", dto.fs_jam_entry, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelas", dto.fs_kd_kelas, SqlDbType.VarChar);
        return dp;
    }
}

