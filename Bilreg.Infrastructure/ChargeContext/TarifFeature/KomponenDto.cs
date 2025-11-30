using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

// resharper disable inconsistentnaming
public record KomponenDto(string fs_kd_detil_tarif, string fs_nm_detil_tarif,
    string fs_kd_grup_detil_tarif, string fs_nm_grup_detil_tarif)
{
    public static KomponenDto FromModel(KomponenType model)
    {
        var result = new KomponenDto(model.KomponenId, model.KomponenName, 
            model.GroupKomponen.GroupKomponenId, model.GroupKomponen.GroupKomponenName);
        return result;
    }

    public KomponenType ToModel(IEnumerable<SatTugasType> listSatTugas)
    {
        var groupKomponen = new GroupKomponenType(fs_kd_grup_detil_tarif, fs_nm_grup_detil_tarif);
        var result = new KomponenType(fs_kd_detil_tarif, fs_nm_detil_tarif, groupKomponen, listSatTugas);
        return result;
    }
}

public record KomponenSatTugasDto(string fs_kd_detil_tarif, 
    string fs_kd_sat_tugas, string fs_nm_sat_tugas,
    string fs_kd_profesi, string fs_nm_profesi)
{
    public static KomponenSatTugasDto FromModel(string komponenId, SatTugasType model)
    {
        var result = new KomponenSatTugasDto(komponenId, model.SatTugasId, model.SatTugasName,
            model.Profesi.ProfesiId, model.Profesi.ProfesiName);
        return result;
    }

    public KomponenSatTugasDto ToModel()
    {
        var profesi = new ProfesiType(fs_kd_profesi, fs_nm_profesi);
        var result = new KomponenSatTugasDto(fs_kd_detil_tarif, fs_kd_sat_tugas, fs_nm_sat_tugas,
            fs_kd_profesi, fs_nm_profesi);
        return result;
    }
}