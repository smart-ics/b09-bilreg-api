using Bilreg.Domain.ChargeContext.TarifFeature;

// ReSharper disable InconsistentNaming
namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public record TarifBarangDto(
    string fs_kd_tarif,
    decimal fn_no_urut,
    string fs_kd_barang,
    string fs_nm_barang,
    string fs_kd_satuan,
    string fs_nm_satuan,
    decimal fn_qty_barang
)
{
    public TarifBarangType ToModel() => new(
        TarifId: fs_kd_tarif,
        NoUrut: (int)fn_no_urut,
        BrgId: fs_kd_barang,
        BrgName: fs_nm_barang,
        SatuanId: fs_kd_satuan,
        SatuanName: fs_nm_satuan,
        QtyBarang: fn_qty_barang
    );

    public static TarifBarangDto FromModel(TarifBarangType model) => new(
        fs_kd_tarif: model.TarifId,
        fn_no_urut: model.NoUrut,
        fs_kd_barang: model.BrgId,
        fs_nm_barang: model.BrgName,
        fs_kd_satuan: model.SatuanId,
        fs_nm_satuan: model.SatuanName,
        fn_qty_barang: model.QtyBarang
    );
}
