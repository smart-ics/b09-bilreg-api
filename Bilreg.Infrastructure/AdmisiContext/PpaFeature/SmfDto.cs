using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

// ReSharper disable InconsistentNaming
public record SmfDto(string fs_kd_smf, string fs_nm_smf)
{
    public static SmfDto FromModel(SmfType model)
    {
        var result = new SmfDto(model.SmfId, model.SmfName);
        return result;
    }

    public SmfType ToModel()
    {
        var result = new SmfType(fs_kd_smf, fs_nm_smf);
        return result;
    }
}