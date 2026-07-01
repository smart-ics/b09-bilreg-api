using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public record JenisInapDto(
    string fs_kd_jenis_inap, 
    string fs_nm_jenis_inap)
{
    public static JenisInapDto FromModel(JenisInapType model)
    {
        var result = new JenisInapDto(
            model.JenisInapId,
            model.JenisInapName);
        return result;
    }

    public JenisInapType ToModel()
    {
        var result = new JenisInapType(
            fs_kd_jenis_inap, 
            fs_nm_jenis_inap);
        return result;
    }
}
