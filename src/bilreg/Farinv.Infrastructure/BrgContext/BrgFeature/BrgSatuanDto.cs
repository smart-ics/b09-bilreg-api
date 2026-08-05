using Farinv.Domain.BrgContext.BrgFeature;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public record BrgSatuanDto(string fs_kd_barang, string fs_kd_satuan, decimal fn_nilai, string fs_nm_satuan)
{
    public static BrgSatuanDto FromModel(string brgId, BrgSatuanType model)
    {
        var result = new BrgSatuanDto(brgId, model.Satuan.SatuanId, model.Konversi, model.Satuan.SatuanName);
        return result;
    }

    public BrgSatuanType ToModel()
    {
        var satuan = new SatuanType(fs_kd_satuan, fs_nm_satuan);
        var result = new BrgSatuanType(satuan, Convert.ToInt32(fn_nilai));
        return result;
    }
}