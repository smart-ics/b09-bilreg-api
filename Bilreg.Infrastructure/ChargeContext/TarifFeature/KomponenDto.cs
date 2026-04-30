using Bilreg.Domain.AccountingContext.CoaFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

// resharper disable inconsistentnaming
public record KomponenDto(string fs_kd_detil_tarif, string fs_nm_detil_tarif,
    string fs_kd_grup_detil_tarif, string fs_kd_rek_pdpt, string fs_kd_rek_diskon, 
    string fs_nm_grup_detil_tarif, string fs_nm_rek_pdpt, string fs_nm_rek_diskon)
{
    public static KomponenDto FromModel(KomponenType model)
    {
        var result = new KomponenDto(model.KomponenId, model.KomponenName, 
            model.GroupKomponen.GroupKomponenId, model.RekPdpt.CoaId, model.RekDiskon.CoaId, 
            model.GroupKomponen.GroupKomponenName, model.RekPdpt.CoaName, model.RekDiskon.CoaName);
        return result;
    }

    public KomponenType ToModel(IEnumerable<SatTugasType> listSatTugas)
    {
        var groupKomponen = new GroupKomponenType(fs_kd_grup_detil_tarif, fs_nm_grup_detil_tarif);
        var rekPdpt = new CoaType(fs_kd_rek_pdpt, fs_nm_rek_pdpt, CoaTipeType.Default);
        var rekDiskon = new CoaType(fs_kd_rek_diskon, fs_nm_rek_diskon, CoaTipeType.Default);
        var result = new KomponenType(fs_kd_detil_tarif, fs_nm_detil_tarif, groupKomponen,
            rekPdpt, rekDiskon, listSatTugas);
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

    public SatTugasType ToModel()
    {
        var profesi = new ProfesiType(fs_kd_profesi, fs_nm_profesi);
        var result = new SatTugasType(fs_kd_sat_tugas, fs_nm_sat_tugas, profesi);
        return result;
    }
}