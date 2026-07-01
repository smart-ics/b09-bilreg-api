using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Domain.Shared.Helpers;
using Nuna.Lib.ValidationHelper;

namespace Farinv.Infrastructure.InventoryContext.StokFeature;
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
    public StokBukuType ToModel()
    {
        var trsReffDateTime = $"{fd_tgl_jam_mutasi}".ToDate(DateFormatEnum.YMD_HMS);
        var trsReff = new TrsReffType(fs_kd_mutasi, trsReffDateTime);
        var result = new StokBukuType(fs_kd_trs, 0, (int)fn_stok_in, (int)fn_stok_out,
            trsReff, fs_kd_jenis_mutasi, trsReffDateTime, ModelStateEnum.Unchange);
        return result;
    }
};