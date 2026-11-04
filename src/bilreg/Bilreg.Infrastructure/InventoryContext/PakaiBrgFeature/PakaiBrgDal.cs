using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.InventoryContext.PakaiBrgFeature;

public interface IPakaiBrgDal :
    IInsert<PakaiBrgDto>,
    IUpdate<PakaiBrgDto>,
    IGetData<PakaiBrgDto, IPakaiBrgKey>,
    IListData<PakaiBrgDto, ILayananKey, Periode>
{
    IEnumerable<PakaiBrgStokLokasiRowDto> ListStokLokasi(ILayananKey layanan);
}

public class PakaiBrgDal : IPakaiBrgDal
{
    private readonly DatabaseOptions _opt;

    public PakaiBrgDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PakaiBrgDto dto)
    {
        const string sql = """
            INSERT INTO tb_trs_pakai (
                fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas,
                fd_tgl_void, fs_jam_void, fs_kd_petugas_void,
                fs_kd_layanan, fs_kd_petugas_approved, fs_kd_reg,
                fs_keterangan1, fs_keterangan2, fn_genkeu,
                fs_kd_layanan_tujuan, fd_potstok_du,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR,
                UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR
            ) VALUES (
                @fs_kd_trs, @fd_tgl_trs, @fs_jam_trs, @fs_kd_petugas,
                @fd_tgl_void, @fs_jam_void, @fs_kd_petugas_void,
                @fs_kd_layanan, @fs_kd_petugas_approved, @fs_kd_reg,
                @fs_keterangan1, @fs_keterangan2, @fn_genkeu,
                @fs_kd_layanan_tujuan, @fd_potstok_du,
                @CRTTGL, @CRTJAM, @CRTIPA, @CRTVER, @CRTUSR,
                @UPDTGL, @UPDJAM, @UPDIPA, @UPDVER, @UPDUSR
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", dto.fs_kd_trs, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_trs", dto.fd_tgl_trs, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_trs", dto.fs_jam_trs, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas", dto.fs_kd_petugas, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_void", dto.fd_tgl_void, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_void", dto.fs_jam_void, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_void", dto.fs_kd_petugas_void, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_approved", dto.fs_kd_petugas_approved, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_reg", dto.fs_kd_reg, SqlDbType.VarChar);
        dp.AddParam("@fs_keterangan1", dto.fs_keterangan1, SqlDbType.VarChar);
        dp.AddParam("@fs_keterangan2", dto.fs_keterangan2, SqlDbType.VarChar);
        dp.AddParam("@fn_genkeu", dto.fn_genkeu, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_layanan_tujuan", dto.fs_kd_layanan_tujuan, SqlDbType.VarChar);
        dp.AddParam("@fd_potstok_du", dto.fd_potstok_du, SqlDbType.VarChar);
        dp.AddParam("@CRTTGL", dto.CRTTGL, SqlDbType.VarChar);
        dp.AddParam("@CRTJAM", dto.CRTJAM, SqlDbType.VarChar);
        dp.AddParam("@CRTIPA", dto.CRTIPA, SqlDbType.VarChar);
        dp.AddParam("@CRTVER", dto.CRTVER, SqlDbType.VarChar);
        dp.AddParam("@CRTUSR", dto.CRTUSR, SqlDbType.VarChar);
        dp.AddParam("@UPDTGL", dto.UPDTGL, SqlDbType.VarChar);
        dp.AddParam("@UPDJAM", dto.UPDJAM, SqlDbType.VarChar);
        dp.AddParam("@UPDIPA", dto.UPDIPA, SqlDbType.VarChar);
        dp.AddParam("@UPDVER", dto.UPDVER, SqlDbType.VarChar);
        dp.AddParam("@UPDUSR", dto.UPDUSR, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PakaiBrgDto dto)
    {
        const string sql = """
            UPDATE tb_trs_pakai SET
                fd_tgl_void = @fd_tgl_void,
                fs_jam_void = @fs_jam_void,
                fs_kd_petugas_void = @fs_kd_petugas_void,
                fs_keterangan2 = @fs_keterangan2,
                UPDTGL = @UPDTGL,
                UPDJAM = @UPDJAM,
                UPDUSR = @UPDUSR
            WHERE fs_kd_trs = @fs_kd_trs
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", dto.fs_kd_trs, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_void", dto.fd_tgl_void, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_void", dto.fs_jam_void, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_void", dto.fs_kd_petugas_void, SqlDbType.VarChar);
        dp.AddParam("@fs_keterangan2", dto.fs_keterangan2, SqlDbType.VarChar);
        dp.AddParam("@UPDTGL", dto.UPDTGL, SqlDbType.VarChar);
        dp.AddParam("@UPDJAM", dto.UPDJAM, SqlDbType.VarChar);
        dp.AddParam("@UPDUSR", dto.UPDUSR, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PakaiBrgDto GetData(IPakaiBrgKey key)
    {
        const string sql = """
            SELECT
                aa.fs_kd_trs, aa.fd_tgl_trs, aa.fs_jam_trs, aa.fs_kd_petugas,
                aa.fd_tgl_void, aa.fs_jam_void, aa.fs_kd_petugas_void,
                aa.fs_kd_layanan, aa.fs_kd_petugas_approved, aa.fs_kd_reg,
                aa.fs_keterangan1, aa.fs_keterangan2, aa.fn_genkeu,
                aa.fs_kd_layanan_tujuan, aa.fd_potstok_du,
                aa.CRTTGL, aa.CRTJAM, aa.CRTIPA, aa.CRTVER, aa.CRTUSR,
                aa.UPDTGL, aa.UPDJAM, aa.UPDIPA, aa.UPDVER, aa.UPDUSR,
                ISNULL(bb.fs_nm_layanan, '') AS fs_nm_layanan
            FROM tb_trs_pakai aa
            LEFT JOIN ta_layanan bb ON aa.fs_kd_layanan = bb.fs_kd_layanan
            WHERE aa.fs_kd_trs = @fs_kd_trs
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.PakaiBrgId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PakaiBrgDto>(sql, dp);
    }

    public IEnumerable<PakaiBrgDto> ListData(ILayananKey filterA, Periode filterB)
    {
        const string sql = """
            SELECT
                aa.fs_kd_trs, aa.fd_tgl_trs, aa.fs_jam_trs, aa.fs_kd_petugas,
                aa.fd_tgl_void, aa.fs_jam_void, aa.fs_kd_petugas_void,
                aa.fs_kd_layanan, aa.fs_kd_petugas_approved, aa.fs_kd_reg,
                aa.fs_keterangan1, aa.fs_keterangan2, aa.fn_genkeu,
                aa.fs_kd_layanan_tujuan, aa.fd_potstok_du,
                aa.CRTTGL, aa.CRTJAM, aa.CRTIPA, aa.CRTVER, aa.CRTUSR,
                aa.UPDTGL, aa.UPDJAM, aa.UPDIPA, aa.UPDVER, aa.UPDUSR,
                ISNULL(bb.fs_nm_layanan, '') AS fs_nm_layanan
            FROM tb_trs_pakai aa
            LEFT JOIN ta_layanan bb ON aa.fs_kd_layanan = bb.fs_kd_layanan
            WHERE aa.fs_kd_layanan = @fs_kd_layanan
              AND aa.fd_tgl_trs BETWEEN @Tgl1 AND @Tgl2
            ORDER BY aa.fd_tgl_trs DESC, aa.fs_jam_trs DESC, aa.fs_kd_trs DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan", filterA.LayananId, SqlDbType.VarChar);
        dp.AddParam("@Tgl1", filterB.Tgl1.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@Tgl2", filterB.Tgl2.ToString("yyyy-MM-dd"), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PakaiBrgDto>(sql, dp);
    }

    public IEnumerable<PakaiBrgStokLokasiRowDto> ListStokLokasi(ILayananKey layanan)
    {
        const string sql = """
            SELECT
                aa.fs_kd_barang,
                bb.fs_nm_barang,
                aa.fs_kd_satuan,
                ISNULL(dd.fs_nm_satuan, '') AS fs_nm_satuan,
                ISNULL(cc.fs_kd_grup_rek_dk, '') AS fs_kd_grup_rek_dk,
                aa.fs_no_batch,
                aa.fd_tgl_ed,
                aa.fn_hpp,
                aa.fn_qty
            FROM tb_stok aa
            INNER JOIN tb_barang bb ON aa.fs_kd_barang = bb.fs_kd_barang
            LEFT JOIN tb_satuan dd ON aa.fs_kd_satuan = dd.fs_kd_satuan
            LEFT JOIN tb_grup_rek cc ON bb.fs_kd_grup_rek = cc.fs_kd_grup_rek
            WHERE aa.fs_kd_layanan = @fs_kd_layanan
              AND aa.fn_qty > 0
              AND bb.fb_aktif = 1
            ORDER BY bb.fs_nm_barang ASC, aa.fd_tgl_ed ASC, aa.fs_kd_trs ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan", layanan.LayananId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PakaiBrgStokLokasiRowDto>(sql, dp);
    }
}
