using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.PakaiBrgFeature;

// ReSharper disable InconsistentNaming
public class PakaiBrgItemDto
{
    public string fs_kd_trs { get; set; } = string.Empty;
    public decimal fn_no_urut { get; set; } = 0;
    public string fs_kd_barang { get; set; } = string.Empty;
    public decimal fn_stok_current { get; set; } = 0;
    public string fs_kd_satuan_current { get; set; } = " ";
    public decimal fn_qty { get; set; } = 0;
    public string fs_kd_satuan { get; set; } = " ";
    public string fd_tgl_ed { get; set; } = "3000-01-01";
    public string fs_no_batch { get; set; } = " ";
    public string fs_keterangan { get; set; } = " ";
    public bool fb_void { get; set; } = false;
    public decimal fn_hpp { get; set; } = 0;
    public decimal fn_hna { get; set; } = 0;
    public decimal fn_hpp_real { get; set; } = 0;
    public decimal fn_sub_total { get; set; } = 0;
    public string CRTTGL { get; set; } = "3000-01-01";
    public string CRTJAM { get; set; } = "00:00:00";
    public string CRTIPA { get; set; } = " ";
    public string CRTVER { get; set; } = " ";
    public string CRTUSR { get; set; } = " ";
    public string UPDTGL { get; set; } = "3000-01-01";
    public string UPDJAM { get; set; } = "00:00:00";
    public string UPDIPA { get; set; } = " ";
    public string UPDVER { get; set; } = " ";
    public string UPDUSR { get; set; } = " ";
    public string fs_nm_barang { get; set; } = string.Empty;
    public string fs_nm_satuan { get; set; } = string.Empty;

    public static PakaiBrgItemDto FromModel(string pakaiBrgId, PakaiBrgItemModel model, DateTime now = default, string user = "")
    {
        var timestamp = now == default ? DateTime.Now : now;
        var userId = string.IsNullOrWhiteSpace(user) ? " " : user;
        return new PakaiBrgItemDto
        {
            fs_kd_trs = pakaiBrgId,
            fn_no_urut = model.NoUrut,
            fs_kd_barang = model.Brg?.BrgId ?? string.Empty,
            fs_nm_barang = model.Brg?.BrgName ?? string.Empty,
            fn_stok_current = model.StokCurrent,
            fs_kd_satuan_current = model.Satuan?.SatuanId ?? " ",
            fn_qty = model.Qty,
            fs_kd_satuan = model.Satuan?.SatuanId ?? " ",
            fs_nm_satuan = model.Satuan?.SatuanName ?? string.Empty,
            fd_tgl_ed = LegacyStockDateHelper.ToLegacyEdString(model.TglEd),
            fs_no_batch = string.IsNullOrWhiteSpace(model.NoBatch) ? " " : model.NoBatch,
            fs_keterangan = string.IsNullOrWhiteSpace(model.Keterangan) ? " " : model.Keterangan,
            fb_void = model.IsVoid,
            fn_hpp = model.Hpp,
            fn_hna = model.Hpp,
            fn_hpp_real = model.Hpp,
            fn_sub_total = model.SubTotal,
            CRTTGL = LegacyStockDateHelper.FormatDate(timestamp),
            CRTJAM = LegacyStockDateHelper.FormatTime(timestamp),
            CRTUSR = userId,
            UPDTGL = "3000-01-01",
            UPDJAM = "00:00:00",
            UPDUSR = " "
        };
    }

    public PakaiBrgItemModel ToModel()
    {
        var brg = new BrgReff(fs_kd_barang?.Trim() ?? string.Empty, fs_nm_barang?.Trim() ?? string.Empty);
        var satuanId = fs_kd_satuan?.Trim() ?? string.Empty;
        var satuanName = string.IsNullOrWhiteSpace(fs_nm_satuan) ? satuanId : fs_nm_satuan.Trim();
        var satuan = new SatuanReff(satuanId, satuanName);
        var tglEd = LegacyStockDateHelper.ParseLegacyDate(fd_tgl_ed);

        return PakaiBrgItemModel.Rehydrate(
            noUrut: (int)fn_no_urut,
            brg: brg,
            satuan: satuan,
            stokCurrent: fn_stok_current,
            qty: fn_qty,
            noBatch: fs_no_batch?.Trim() ?? string.Empty,
            tglEd: tglEd,
            hpp: fn_hpp,
            subTotal: fn_sub_total,
            keterangan: fs_keterangan?.Trim() ?? string.Empty,
            isVoid: fb_void);
    }
}

public class PakaiBrgStokLokasiRowDto
{
    public string fs_kd_barang { get; set; } = string.Empty;
    public string fs_nm_barang { get; set; } = string.Empty;
    public string fs_kd_satuan { get; set; } = string.Empty;
    public string fs_nm_satuan { get; set; } = string.Empty;
    public string fs_kd_grup_rek_dk { get; set; } = string.Empty;
    public string fs_no_batch { get; set; } = string.Empty;
    public string fd_tgl_ed { get; set; } = string.Empty;
    public decimal fn_hpp { get; set; } = 0;
    public decimal fn_qty { get; set; } = 0;
}
