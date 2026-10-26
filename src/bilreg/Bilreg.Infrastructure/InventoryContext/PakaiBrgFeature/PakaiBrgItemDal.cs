using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.PakaiBrgFeature;

public interface IPakaiBrgItemDal :
    IInsert<PakaiBrgItemDto>,
    IDelete<IPakaiBrgKey>,
    IListData<PakaiBrgItemDto, IPakaiBrgKey>
{
    void Insert(IEnumerable<PakaiBrgItemDto> list);
    void UpdateVoid(IPakaiBrgKey key);
}

public class PakaiBrgItemDal : IPakaiBrgItemDal
{
    private readonly DatabaseOptions _opt;

    public PakaiBrgItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PakaiBrgItemDto dto)
    {
        const string sql = """
            INSERT INTO tb_trs_pakai2 (
                fs_kd_trs, fn_no_urut, fs_kd_barang,
                fn_stok_current, fs_kd_satuan_current,
                fn_qty, fs_kd_satuan, fd_tgl_ed,
                fs_no_batch, fs_keterangan, fb_void,
                fn_hpp, fn_hna, fn_hpp_real, fn_sub_total,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR,
                UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR
            ) VALUES (
                @fs_kd_trs, @fn_no_urut, @fs_kd_barang,
                @fn_stok_current, @fs_kd_satuan_current,
                @fn_qty, @fs_kd_satuan, @fd_tgl_ed,
                @fs_no_batch, @fs_keterangan, @fb_void,
                @fn_hpp, @fn_hna, @fn_hpp_real, @fn_sub_total,
                @CRTTGL, @CRTJAM, @CRTIPA, @CRTVER, @CRTUSR,
                @UPDTGL, @UPDJAM, @UPDIPA, @UPDVER, @UPDUSR
            )
            """;

        var dp = CreateDynamicParameters(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Insert(IEnumerable<PakaiBrgItemDto> list)
    {
        foreach (var item in list)
        {
            Insert(item);
        }
    }

    public void Delete(IPakaiBrgKey key)
    {
        const string sql = """
            DELETE FROM tb_trs_pakai2
            WHERE fs_kd_trs = @fs_kd_trs
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.PakaiBrgId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void UpdateVoid(IPakaiBrgKey key)
    {
        const string sql = """
            UPDATE tb_trs_pakai2
            SET fb_void = 1
            WHERE fs_kd_trs = @fs_kd_trs
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.PakaiBrgId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PakaiBrgItemDto> ListData(IPakaiBrgKey filter)
    {
        const string sql = """
            SELECT
                aa.fs_kd_trs, aa.fn_no_urut, aa.fs_kd_barang,
                aa.fn_stok_current, aa.fs_kd_satuan_current,
                aa.fn_qty, aa.fs_kd_satuan, aa.fd_tgl_ed,
                aa.fs_no_batch, aa.fs_keterangan, aa.fb_void,
                aa.fn_hpp, aa.fn_hna, aa.fn_hpp_real, aa.fn_sub_total,
                aa.CRTTGL, aa.CRTJAM, aa.CRTIPA, aa.CRTVER, aa.CRTUSR,
                aa.UPDTGL, aa.UPDJAM, aa.UPDIPA, aa.UPDVER, aa.UPDUSR,
                ISNULL(bb.fs_nm_barang, '') AS fs_nm_barang,
                ISNULL(cc.fs_nm_satuan, '') AS fs_nm_satuan
            FROM tb_trs_pakai2 aa
            LEFT JOIN tb_barang bb ON aa.fs_kd_barang = bb.fs_kd_barang
            LEFT JOIN tb_satuan cc ON aa.fs_kd_satuan = cc.fs_kd_satuan
            WHERE aa.fs_kd_trs = @fs_kd_trs
            ORDER BY aa.fn_no_urut ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", filter.PakaiBrgId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PakaiBrgItemDto>(sql, dp);
    }

    private static DynamicParameters CreateDynamicParameters(PakaiBrgItemDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", dto.fs_kd_trs, SqlDbType.VarChar);
        dp.AddParam("@fn_no_urut", dto.fn_no_urut, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_barang", dto.fs_kd_barang, SqlDbType.VarChar);
        dp.AddParam("@fn_stok_current", dto.fn_stok_current, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_satuan_current", dto.fs_kd_satuan_current, SqlDbType.VarChar);
        dp.AddParam("@fn_qty", dto.fn_qty, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_satuan", dto.fs_kd_satuan, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_ed", dto.fd_tgl_ed, SqlDbType.VarChar);
        dp.AddParam("@fs_no_batch", dto.fs_no_batch, SqlDbType.VarChar);
        dp.AddParam("@fs_keterangan", dto.fs_keterangan, SqlDbType.VarChar);
        dp.AddParam("@fb_void", dto.fb_void, SqlDbType.Bit);
        dp.AddParam("@fn_hpp", dto.fn_hpp, SqlDbType.Decimal);
        dp.AddParam("@fn_hna", dto.fn_hna, SqlDbType.Decimal);
        dp.AddParam("@fn_hpp_real", dto.fn_hpp_real, SqlDbType.Decimal);
        dp.AddParam("@fn_sub_total", dto.fn_sub_total, SqlDbType.Decimal);
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
        return dp;
    }
}
