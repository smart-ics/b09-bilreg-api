using Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;

namespace Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;

public class JaminanModel: IJaminanKey
{
    public string JaminanId { get; private set; }
    public string JaminanName { get; private set; }
    public string Alamat1 { get; private set; }
    public string Alamat2 { get; private set; }
    public string Kota { get; private set; }
    public bool IsAktif { get; private set; }
    public string CaraBayarDkId { get; private set; }
    public string CaraBayarDkName { get; private set; }
    public string GrupJaminanId { get; private set; }
    public string GrupJaminanName { get; private set; }
    public string BenefitMou { get; private set; }

    public void SetAktif() => IsAktif = true;
    public void UnSetAktif() => IsAktif = false;
    public void SetBenefitMou(string benefitMou) => BenefitMou = benefitMou;

    public void SetAlamat(string alamat1, string alamat2, string kota)
    {
        Alamat1 = alamat1;
        Alamat2 = alamat2;
        Kota = kota;
    }

    public void SetCaraBayar(CaraBayarDkModel caraBayarDk)
    {
        CaraBayarDkId = caraBayarDk.CaraBayarDkId;
        CaraBayarDkName = caraBayarDk.CaraBayarDkName;
    }

    public void SetGrupJaminan(GrupJaminanModel grupJaminan)
    {
        GrupJaminanId = grupJaminan.GrupJaminanId;
        GrupJaminanName = grupJaminan.GrupJaminanName;
    }

    public JaminanModel(string id, string name)
    {
        JaminanId = id;
        JaminanName = name;
        Alamat1 = string.Empty;
        Alamat2 = string.Empty;
        Kota = string.Empty;
        IsAktif = true;
        CaraBayarDkId = string.Empty;
        CaraBayarDkName = string.Empty;
        GrupJaminanId = string.Empty;
        GrupJaminanName = string.Empty;
        BenefitMou = string.Empty;
    }
    
    public static JaminanModel Create(string id, string name) => new JaminanModel(id, name);
}

public interface IJaminanKey
{
    string JaminanId { get; }
}