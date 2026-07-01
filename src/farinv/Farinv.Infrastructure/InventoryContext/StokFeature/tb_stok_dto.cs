namespace Farinv.Infrastructure.InventoryContext.StokFeature;

// resharper disable inconsistentnaming
public record tb_stok_dto(
    string fs_kd_trs, string fs_kd_barang, string fs_kd_layanan,
    string fs_kd_po, string fs_kd_do, string fd_tgl_ed, string fs_no_batch,
    decimal fn_qty_in, decimal fn_qty, decimal fn_hpp,
    string fd_tgl_do, string fs_jam_do,
    string fs_kd_mutasi, string fd_tgl_mutasi, string fs_jam_mutasi,
    string fs_kd_satuan, string fs_nm_barang, string fs_nm_layanan);