using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Domain.PasienContext.PasienFeature;

public class PasienModel : IPasienKey
{
    public PasienModel(string pasienId, string pasienName, 
        DateTime tglLahir, GenderType gender, string nickName, string tempatLahir,
        string ibuKandung, GolDarahType golDarah, AlamatType alamat, 
        KelurahanType kelurahan, IdentificationType identitas, 
        List<ContactType> contacts, ContactType contact, 
        PasienKeluargaType pasienKel, StatusKawinDkType statusKawinDk,
        AgamaType agama, SukuType suku, PekerjaanDkType pekerjaanDk,
        PendidikanDkType pendidikanDk, DateTime tglMedRec, bool isAktif)
    {
        PasienId = pasienId;
        PasienName = pasienName;
        TglLahir = tglLahir;
        Gender = gender;
        NickName = nickName;
        TempatLahir = tempatLahir;
        IbuKandung = ibuKandung;
        GolDarah = golDarah;
        Alamat = alamat;
        Kelurahan = kelurahan;
        Identification = identitas;
        Contact = contact;
        PasienKeluarga = pasienKel;
        StatusKawin = statusKawinDk;
        Agama = agama;
        Suku = suku;
        PekerjaanDk = pekerjaanDk;
        PendidikanDk = pendidikanDk;
        TglMedRec = tglMedRec;
        IsAktif = isAktif;  
    }
    
    //  MANDATORY PROPERTIES
    public string PasienId { get; private set; } 
    public string PasienName { get; private set; }
    public DateTime TglLahir { get; private set; }
    public GenderType Gender { get; private set; }
    
    //      personal info
    public string NickName { get; private set; }
    public string TempatLahir { get; private set; }
    public string IbuKandung { get; private set; } 
    public GolDarahType GolDarah { get; private set; } 
    
    //      administrative info
    public AlamatType Alamat { get; private set; } 
    public KelurahanType Kelurahan { get; private set; } 
    public IdentificationType Identification { get; private set; }  
    public ContactType Contact { get; private set; } 
    public PasienKeluargaType PasienKeluarga { get; private set; } 
    
    //      status sosial
    public StatusKawinDkType StatusKawin { get; private set; } 
    public AgamaType Agama { get; private set; } 
    public SukuType Suku { get; private set; } 
    public PekerjaanDkType PekerjaanDk { get; private set; } 
    public PendidikanDkType PendidikanDk { get; private set; } 

    //      olah berkas
    public DateTime TglMedRec { get; private set; } 
    public bool IsAktif { get; private set; }
    public string GetNomorMedrec()
    {
        var pasienId = PasienId;
        var breakPasienId = pasienId[7..]
            .Chunk(2)
            .Select(x => new string(x))
            .ToList();
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

    public void SetAdministrativeInfo(AlamatType alamat, KelurahanType kelurahan,
        IdentificationType identitas, ContactType contact, PasienKeluargaType pasienKeluarga)
    {
        Alamat = alamat;
        Kelurahan = kelurahan;
        Identification = identitas;
        Contact = contact;
        PasienKeluarga = pasienKeluarga;
    }

    public void SetStatusSosial(StatusKawinDkType statusKawin, AgamaType agama,
        SukuType suku, PekerjaanDkType pekerjaanDk, PendidikanDkType pendidikanDk)
    {
        StatusKawin = statusKawin;
        Agama = agama;
        Suku = suku;
        PekerjaanDk = pekerjaanDk;
        PendidikanDk = pendidikanDk;
    }

    public void SetTglMedRec(DateTime tglMedRec)
    {
        TglMedRec = tglMedRec;
    }
    public PasienViewType ToViewType() => new PasienViewType(PasienId, GetNomorMedrec(), PasienName, TglLahir, Gender);
}
