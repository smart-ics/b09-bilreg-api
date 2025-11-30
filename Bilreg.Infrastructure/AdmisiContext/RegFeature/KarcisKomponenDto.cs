using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;

//  resharper disable inconsistentnaming
namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public record KarcisKomponenDto(
    string fs_kd_karcis,
    string fs_kd_detil_tarif,
    decimal fn_tarif,
    string fs_nm_detil_tarif)
{
    public static KarcisKomponenDto ToDto(string karcisId, KarcisKomponenType model)
    {
        return new KarcisKomponenDto(
            karcisId,
            model.KomponenTarif.KomponenId,
            model.Nilai,
            model.KomponenTarif.KomponenName);
    }

    public KarcisKomponenType ToModel()
    {
        return new KarcisKomponenType(
            new KomponenReff(fs_kd_detil_tarif, fs_nm_detil_tarif),
            fn_tarif);
    }
}