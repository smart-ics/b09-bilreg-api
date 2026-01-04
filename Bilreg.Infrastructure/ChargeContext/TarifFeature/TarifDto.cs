using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;

// ReSharper disable InconsistentNaming
namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public record TarifDto(
    string fs_kd_tarif,
    string fs_nm_tarif,
    string fs_kd_grup_tarif,
    string fs_kd_grup_tarif_dk,
    string fs_kd_jenis_tarif,
    string fs_kd_rekap_cetak_tarif,
    string fs_nm_grup_tarif,
    string fs_nm_grup_tarif_dk,
    string fs_nm_jenis_tarif,
    string fs_nm_rekap_cetak_tarif
)
{
    public static TarifDto FromModel(TarifType model)
    {
        var result = new TarifDto(
            model.TarifId,
            model.TarifName,
            model.GroupTarif.GroupTarifId,
            model.GroupTarifDk.GroupTarifDkId,
            model.JenisTarif.JenisTarifId,
            model.RekapCetak.RekapCetakId,
            model.GroupTarif.GroupTarifName,
            model.GroupTarifDk.GroupTarifDkName,
            model.JenisTarif.JenisTarifName,
            model.RekapCetak.RekapCetakName
        );
        return result;
    }

    public TarifType ToModel()
    {
        var groupTarifDk = new GroupTarifDkType(fs_kd_grup_tarif_dk, fs_nm_grup_tarif_dk);
        var groupTarif = new GroupTarifType(fs_kd_grup_tarif, fs_nm_grup_tarif);
        var jnsTarif = new JenisTarifType(fs_kd_jenis_tarif, fs_nm_jenis_tarif, 0);
        var rekapCetak = new RekapCetakReff(fs_kd_rekap_cetak_tarif, fs_nm_rekap_cetak_tarif);
        return new TarifType(fs_kd_tarif, fs_nm_tarif, groupTarif,
            groupTarifDk, jnsTarif, rekapCetak);
    }
}

