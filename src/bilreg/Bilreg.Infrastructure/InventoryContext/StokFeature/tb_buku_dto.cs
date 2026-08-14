using Bilreg.Domain.InventoryContext.StokFeature;
using Bilreg.Domain.Shared.Helpers;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.InventoryContext.StokFeature;
//  resharper disable inconsistentnaming
public record tb_buku_dto(
    string fs_kd_trs,
    string fs_kd_barang,
    string fs_kd_layanan,
    string fs_kd_po,
    string fs_kd_do,
    string fd_tgl_ed,
    string fs_no_batch,
    
    decimal fn_stok_in,
    decimal fn_stok_out,
    decimal fn_hpp,
    
    string fs_kd_mutasi,
    string fd_tgl_mutasi,
    string fs_jam_mutasi,
    string fd_tgl_jam_mutasi,
    string fs_kd_jenis_mutasi,
    string fs_kd_satuan,

    string fs_nm_barang,
    string fs_nm_layanan)
{
};