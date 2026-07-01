using Farinv.Domain.BrgContext.KlasifikasiFeature;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public record JenisDto(string fs_kd_jenis_barang, string fs_nm_jenis_barang)
{
    public static JenisDto FromModel(JenisType model)
    {
        return new JenisDto(model.JenisId, model.JenisName);
    }

    public JenisType ToModel()
    {
        return JenisType.Create(fs_kd_jenis_barang, fs_nm_jenis_barang);
    }
}
