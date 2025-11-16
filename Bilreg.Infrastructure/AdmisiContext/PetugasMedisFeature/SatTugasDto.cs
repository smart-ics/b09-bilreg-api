using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;

namespace Bilreg.Infrastructure.AdmisiContext.PetugasMedisFeature;

// resharper disable inconsistentnaming
public record SatTugasDto(string fs_kd_sat_tugas, string fs_nm_sat_tugas, bool fb_sat_medis)
{
    public static SatTugasDto FromModel(SatTugasType model)
    {
        var result = new SatTugasDto(model.SatTugasId, model.SatTugasName, model.IsMedis);
        return result;
    }

    public SatTugasType ToModel()
    {
        var result = new SatTugasType(fs_kd_sat_tugas, fs_nm_sat_tugas, fb_sat_medis);
        return result;
    }
}