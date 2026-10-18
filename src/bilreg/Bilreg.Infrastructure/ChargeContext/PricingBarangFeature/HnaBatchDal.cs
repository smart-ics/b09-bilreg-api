using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

public interface IHnaBatchDal
{
    /// <summary>
    /// Menyimpan batch HNA (header <c>tb_trs_hna</c>, baris <c>tb_trs_hna2</c>) dan memutakhirkan
    /// tanggal kedaluwarsa snapshot aktif lama secara atomik dalam satu <see cref="IDbTransaction"/>.
    /// </summary>
    void SaveBatch(HnaBatchModel model);
}

/// <summary>
/// Data access header batch HNA pada tabel legacy <c>tb_trs_hna</c> dan orkestrasi transaksi atomik
/// penerbitan batch (ARCHITECTURE TD-04).
/// </summary>
public class HnaBatchDal : IHnaBatchDal
{
    private readonly DatabaseOptions _opt;
    private readonly IHnaBatchItemDal _itemDal;

    public HnaBatchDal(IOptions<DatabaseOptions> opt, IHnaBatchItemDal itemDal)
    {
        _opt = opt.Value;
        _itemDal = itemDal;
    }

    public void SaveBatch(HnaBatchModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.Validate();

        var now = DateTime.Now;
        var header = HnaBatchDto.FromModel(model, now);
        var items = model.ListItem
            .Select(x => HnaBatchItemDto.FromModel(x, now))
            .ToList();

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            InsertHeader(conn, tx, header);
            _itemDal.SaveBatch(conn, tx, model.HnaBatchId, items);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static void InsertHeader(SqlConnection conn, SqlTransaction tx, HnaBatchDto dto)
    {
        const string sql = """
            INSERT INTO tb_trs_hna (
                fs_kd_trs,
                fd_tgl_trs,
                fs_jam_trs,
                fs_kd_petugas,
                fd_tgl_void,
                fs_jam_void,
                fs_kd_petugas_void,
                CRTTGL,
                CRTJAM,
                CRTUSR,
                UPDTGL,
                UPDJAM,
                UPDUSR
            ) VALUES (
                @fs_kd_trs,
                @fd_tgl_trs,
                @fs_jam_trs,
                @fs_kd_petugas,
                @fd_tgl_void,
                @fs_jam_void,
                @fs_kd_petugas_void,
                @CRTTGL,
                @CRTJAM,
                @CRTUSR,
                @UPDTGL,
                @UPDJAM,
                @UPDUSR
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
        dp.AddParam("@CRTTGL", dto.CRTTGL, SqlDbType.VarChar);
        dp.AddParam("@CRTJAM", dto.CRTJAM, SqlDbType.VarChar);
        dp.AddParam("@CRTUSR", dto.CRTUSR, SqlDbType.VarChar);
        dp.AddParam("@UPDTGL", dto.UPDTGL, SqlDbType.VarChar);
        dp.AddParam("@UPDJAM", dto.UPDJAM, SqlDbType.VarChar);
        dp.AddParam("@UPDUSR", dto.UPDUSR, SqlDbType.VarChar);

        conn.Execute(sql, dp, tx);
    }
}
