using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.KomponenTarifAgg;

public class KomponenTarifDto() : KomponenTarifModel(string.Empty, String.Empty)
{
    public string fs_kd_detil_tarif { get => KomponenId; set => KomponenId = value; }
    public string fs_nm_detil_tarif { get => KomponenName; set => KomponenName = value; }
}