using Bilreg.Domain.BillContext.TindakanSub.TarifFeature;

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
        return new TarifType(fs_kd_tarif, fs_nm_tarif);
    }
}  
  

