using Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;

namespace Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;

public class JaminanModel : IJaminanKey
{
    public JaminanModel(string jaminanId, string jaminanName,
        AddressType address, bool isAktif,
        CaraBayarDkModel caraBayarDk, GrupJaminanModel grupJaminan,
        string benefitMou)
    {
        if (jaminanId == string.Empty ^ jaminanName == string.Empty)
            throw new ArgumentException("Jaminan invalid");

        JaminanId = jaminanId;
        JaminanName = jaminanName;
        Address = address;
        IsAktif = isAktif;
        CaraBayarDk = caraBayarDk;
        GrupJaminan = grupJaminan.ToViewType();
        BenefitMou = benefitMou;
    }

    public static JaminanModel Default => new JaminanModel(
        string.Empty, string.Empty,
        AddressType.Default, false,
        CaraBayarDkModel.Default, GrupJaminanModel.Default,
        string.Empty);
    
    public JaminanViewType ToViewType() => new JaminanViewType(
        JaminanId, JaminanName, 
        CaraBayarDk.CaraBayarDkName, 
        GrupJaminan.GrupJaminanName);
    
    public string JaminanId { get; private set; }
    public string JaminanName { get; private set; }
    public AddressType Address { get; private set; }
    public bool IsAktif { get; private set; }
    public CaraBayarDkModel CaraBayarDk { get; private set; }
    public GrupJaminanViewType GrupJaminan { get; private set; }
    public string BenefitMou { get; private set; }
    
}

public record JaminanViewType(
    string JaminanId, string JaminanName,
    string CaraBayarName,
    string GrupJaminanName);