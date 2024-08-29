using Bilreg.Domain.PasienContext.DemografiSub.KotaAgg;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KotaAgg;

public class KotaDto
{
    public string fs_kd_kota { get; set; }
    public string fs_nm_kota { get; set; }
    
    public KotaModel ToModel() => new KotaModel(fs_kd_kota, fs_nm_kota);
}