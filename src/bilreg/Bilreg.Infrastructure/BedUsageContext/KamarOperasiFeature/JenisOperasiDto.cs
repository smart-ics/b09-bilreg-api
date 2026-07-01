using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record JenisOperasiDto(string fs_kd_jenis_operasi, string fs_nm_jenis_operasi)
{
    public static JenisOperasiDto FromModel(JenisOperasiType model)
    => new(model.JenisOperasiId, model.JenisOperasiName);
    
    public JenisOperasiType ToModel()
    => new JenisOperasiType(fs_kd_jenis_operasi, fs_nm_jenis_operasi);
}