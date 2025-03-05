using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.AgamaAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.PendidikanDkAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.StatusKawinDkAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.SukuAgg;
using CommunityToolkit.Diagnostics;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public class PasienModel : IPasienKey
{
    public static IPasienKey Key(string pasienId) 
        => new PasienModel(pasienId, string.Empty, new DateTime(2000,1,1), GenderType.Default);
    
    public PasienModel(string pasienId, string pasienName, 
        DateTime tglLahir, GenderType gender)
    {
        Guard.IsNotEmpty(pasienId);
        Guard.IsNotEmpty(pasienName);
        Guard.IsGreaterThan(tglLahir, new DateTime(1900, 1, 1));
        
        PasienId = pasienId;
        PasienName = pasienName;
        TglLahir = tglLahir;
        Gender = gender;
    }

    
    //  MANDATORY PROPERTIES
    public string PasienId { get; private set; } 
    public string PasienName { get; private set; }
    public DateTime TglLahir { get; private set; }
    public GenderType Gender { get; private set; }
    
    //  OPTIONAL PROPERTIES
    //      personal info
    public string NickName { get; private set; } = string.Empty;
    public string TempatLahir { get; private set; } = string.Empty;
    public string IbuKandung { get; private set; } = string.Empty;
    public GolDarahType GolDarah { get; private set; } = GolDarahType.Default;
    
    //      administrative info
    public AddressType Address { get; private set; } = AddressType.Default;
    public KelurahanModel Kelurahan { get; private set; } = KelurahanModel.Default;
    public IdentityType Identity { get; private set; }  = IdentityType.Default;
    public ContactType Contact { get; private set; } = ContactType.Default;
    public KeluargaType Keluarga { get; private set; } = KeluargaType.Default;
    
    //      status sosial
    public StatusKawinDkModel StatusKawin { get; private set; } = StatusKawinDkModel.Default;
    public AgamaModel Agama { get; private set; } = AgamaModel.Default;
    public SukuModel Suku { get; private set; } = SukuModel.Default;
    public PekerjaanDkModel Pekerjaan { get; private set; } = PekerjaanDkModel.Default;
    public PendidikanDkModel Pendidikan { get; private set; } = PendidikanDkModel.Default;
    //      olah berkas
    public DateTime TglMedRec { get; private set; } = DateTime.Now.Date;
    public bool IsAktif { get; private set; } = true;
    public string GetNomorMedrec()
    {
        var pasienId = PasienId;
        var breakPasienId = pasienId[7..]
            .Chunk(2)
            .Select(x => new string(x))
            .ToList();
        //  merge breakPasienId to string
        return breakPasienId.Join("-");
    }

    public void SetPersonalInfo(string nickName, string tempatLahir, string ibuKandung,
        GolDarahType golDarah)
    {
        NickName = nickName;
        TempatLahir = tempatLahir;
        IbuKandung = ibuKandung;
        GolDarah = golDarah;
    }

    public void SetAdministrativeInfo(AddressType address, KelurahanModel kelurahan,
        IdentityType identitas, ContactType contact, KeluargaType keluarga)
    {
        Address = address;
        Kelurahan = kelurahan;
        Identity = identitas;
        Contact = contact;
        Keluarga = keluarga;
    }

    public void SetStatusSosial(StatusKawinDkModel statusKawin, AgamaModel agama,
        SukuModel suku, PekerjaanDkModel pekerjaan, PendidikanDkModel pendidikan)
    {
        StatusKawin = statusKawin;
        Agama = agama;
        Suku = suku;
        Pekerjaan = pekerjaan;
        Pendidikan = pendidikan;
    }

    public void SetTglMedRec(DateTime tglMedRec)
    {
        TglMedRec = tglMedRec;
    }
}
