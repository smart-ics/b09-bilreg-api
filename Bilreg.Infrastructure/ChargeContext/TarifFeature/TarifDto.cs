using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.TarifAgg;

public record TarifDto(
    string fs_kd_tarif,
    string fs_nm_tarif,
    string fs_kd_grup_tarif,
    string fs_kd_grup_tarif_dk,
    string fs_kd_jenis_tarif,
    string fs_nm_grup_tarif,
    string fs_nm_grup_tarif_dk,
    string fs_nm_jenis_tarif
)
{
    public TarifType ToModel()
    {
        var groupTarifDk = new GroupTarifDkType(fs_kd_grup_tarif_dk, fs_nm_grup_tarif_dk);
        var groupTarif = new GroupTarifType(fs_kd_grup_tarif, fs_nm_grup_tarif);
        var jnsTarif = new JenisTarifType(fs_kd_jenis_tarif, fs_nm_jenis_tarif, 0);
        return new TarifType(fs_kd_tarif, fs_nm_tarif, groupTarif,
        groupTarifDk, jnsTarif);
    }
}


