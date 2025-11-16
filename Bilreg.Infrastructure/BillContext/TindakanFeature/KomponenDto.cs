using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.BillContext.TindakanFeature;

namespace Bilreg.Infrastructure.BillContext.TindakanFeature;

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
    string fs_kd_sat_tugas, string fs_nm_sat_tugas, bool fb_sat_medis)
{
    public static KomponenSatTugasDto FromModel(string komponenId, SatTugasType model)
    {
        var result = new KomponenSatTugasDto(komponenId, model.SatTugasId, model.SatTugasName, model.IsMedis);
        return result;
    }

    public SatTugasType ToModel()
    {
        var result = new SatTugasType(fs_kd_sat_tugas, fs_nm_sat_tugas, fb_sat_medis);
        return result;
    }
}