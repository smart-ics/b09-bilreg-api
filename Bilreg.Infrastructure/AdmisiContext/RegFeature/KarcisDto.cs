using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;

//  resharper disable inconsistentnaming
namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public record KarcisDto(
    string fs_kd_karcis,
    string fs_nm_karcis,
    decimal fn_karcis,
    string fs_kd_instalasi_dk,
    string fs_kd_rekap_cetak,
    string fs_kd_tarif,
    bool fb_aktif,
    //
    string fs_nm_instalasi_dk,
    string fs_nm_rekap_cetak,
    string fs_nm_tarif)
{
    public static KarcisDto FromModel(KarcisType model)
    {
        var result = new KarcisDto(
            model.KarcisId, model.KarcisName, model.NilaiKarcis,
            model.InstalasiDk.InstalasiDkId, model.RekapCetak.RekapCetakId, model.DefaultTarif.TarifId,
            model.IsAktif, model.InstalasiDk.InstalasiDkName, model.RekapCetak.RekapCetakName,
            model.DefaultTarif.TarifName);
        return result;
    }

    public KarcisType ToModel(IEnumerable<KarcisKomponenType> listKomponen,
        IEnumerable<LayananReff> listLayanan)
    {
        var result = new KarcisType(fs_kd_karcis, fs_nm_karcis, fb_aktif,
            new InstalasiDkType(fs_kd_instalasi_dk, fs_nm_instalasi_dk),
            new RekapCetakReff(fs_kd_rekap_cetak, fs_nm_rekap_cetak),
            new TarifReff(fs_kd_tarif, fs_nm_tarif), listKomponen, listLayanan);
        return result;
    }
}