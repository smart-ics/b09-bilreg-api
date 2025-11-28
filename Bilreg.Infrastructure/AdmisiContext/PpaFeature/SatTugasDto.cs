using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

// resharper disable inconsistentnaming
public record SatTugasDto(string fs_kd_sat_tugas, string fs_nm_sat_tugas, string fs_kd_profesi, string fs_nm_profesi)
{
    public static SatTugasDto FromModel(SatTugasType model)
    {
        var result = new SatTugasDto(model.SatTugasId, model.SatTugasName, 
            model.Profesi.ProfesiId, model.Profesi.ProfesiName);
        return result;
    }

    public SatTugasType ToModel()
    {
        var profesi = new ProfesiType(fs_kd_profesi, fs_nm_profesi);
        var result = new SatTugasType(fs_kd_sat_tugas, fs_nm_sat_tugas, profesi);
        return result;
    }
}