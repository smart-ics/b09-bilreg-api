using Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

namespace Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;

public class JaminanModel: IJaminanKey
{
    public string JaminanId { get; private set; }
    public string JaminanName { get; private set; }
    public AddressType Address { get; private set; }
    public bool IsAktif { get; private set; }
    public string CaraBayarDkId { get; private set; }
    public string CaraBayarDkName { get; private set; }
    public string GrupJaminanId { get; private set; }
    public string GrupJaminanName { get; private set; }
    public string BenefitMou { get; private set; }


}

public interface IJaminanKey
{
    string JaminanId { get; }
}