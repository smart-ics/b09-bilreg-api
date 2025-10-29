using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;

namespace Bilreg.Domain.PasienContext.PasienFeature;

public class PasienModel : IPasienKey
{
    private readonly List<ContactType> _listContact;
    
    public PasienModel(string pasienId, PersonInfoType person,  
        string nickName, string tempatLahir, GolDarahType golDarah, string namaIbuKandung, 
        AlamatType alamatKtp, KelurahanType kelurahan, 
        //
        IdentitasType kartuKeluarga, 
        IEnumerable<ContactType> listContact, 
        //
        PasienKeluargaType keluarga, 
        //
        AgamaType agama, SukuType suku, StatusKawinDkType statusKawinDk, 
        PendidikanDkType pendidikanDk, PekerjaanDkType pekerjaanDk, 
        //
        DateTime tglMedRec, bool isAktif)
    {
        PasienId = pasienId;
        Person = person;
        NickName = nickName;
        TempatLahir = tempatLahir;
        GolDarah = golDarah;
        NamaIbuKandung = namaIbuKandung;
        
        AlamatKtp = alamatKtp;
        Kelurahan = kelurahan;
        
        KartuKeluarga = kartuKeluarga;
        PasienKeluarga = keluarga;
        StatusKawin = statusKawinDk;
        Agama = agama;
        Suku = suku;
        PekerjaanDk = pekerjaanDk;
        PendidikanDk = pendidikanDk;
        TglMedRec = tglMedRec;
        IsAktif = isAktif;

        _listContact = listContact.ToList();
    }

    public static PasienModel Default => new PasienModel("-",
        PersonInfoType.Default, "-", "-", GolDarahType.Default, "-", 
        AlamatType.Default, KelurahanType.Default, IdentitasType.Default, 
        new List<ContactType>(), PasienKeluargaType.Default, 
        AgamaType.Default, SukuType.Default, StatusKawinDkType.Default, 
        PendidikanDkType.Default, PekerjaanDkType.Default, 
        DateTime.MinValue, false);
    
    public static IPasienKey Key(string id) => new PasienModel(id,
        PersonInfoType.Default, "-", "-", GolDarahType.Default, "-", 
        AlamatType.Default, KelurahanType.Default, IdentitasType.Default, 
        new List<ContactType>(), PasienKeluargaType.Default, 
        AgamaType.Default, SukuType.Default, StatusKawinDkType.Default, 
        PendidikanDkType.Default, PekerjaanDkType.Default, 
        DateTime.MinValue, false);



    #region PROPERTIES
    //      personal info
    public string PasienId { get; private set; } 
    public PersonInfoType Person { get; init; }
    public string NickName { get; private set; }
    public string TempatLahir { get; private set; }
    public GolDarahType GolDarah { get; private set; }
    public string NamaIbuKandung { get; private set; }
    
    //      administrative info
    public AlamatType AlamatKtp { get; private set; }
    public KelurahanType Kelurahan { get; private set; } 
    public IdentitasType KartuKeluarga { get; private set; }
    public IEnumerable<ContactType> ListContact => _listContact;
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
    #endregion
    
    #region BEHAVIOR
    public string GetNomorMedrec()
    {
        var lastEightDigits = PasienId.Length < 8 
            ? PasienId 
            : PasienId.Substring(PasienId.Length - 8, 8);
    
        var shortId = string.Join("-", Enumerable.Range(0, 4)
            .Select(i => lastEightDigits.Substring(i * 2, 2)));

        return shortId;        
    }

    public void SetAdministrativeInfo(AlamatType alamatKtp,
        KelurahanType kelurahan, IdentitasType kartuKeluarga, 
        IEnumerable<ContactType> listContact, PasienKeluargaType keluarga)
    {
        Guard.Against.Null(alamatKtp, nameof(alamatKtp));
        Guard.Against.Null(kelurahan, nameof(kelurahan));
        Guard.Against.Null(kartuKeluarga, nameof(kartuKeluarga));
        Guard.Against.Null(keluarga, nameof(keluarga));
        
        var listContactFetched = listContact.ToList();
        Guard.Against.Null(listContactFetched, nameof(listContact));

        if (kartuKeluarga.JenisId != "KK")
            throw new ArgumentException("Jenis Kartu Keluarga harus KK");

        AlamatKtp = alamatKtp;
        Kelurahan = kelurahan;
        KartuKeluarga = kartuKeluarga;
        PasienKeluarga = keluarga;
        _listContact.Clear();
        _listContact.AddRange(listContactFetched);
    }

    public void SetStatusSosial(StatusKawinDkType statusKawin, AgamaType agama,
        SukuType suku, PekerjaanDkType pekerjaanDk, PendidikanDkType pendidikanDk)
    {
        Guard.Against.Null(statusKawin, nameof(statusKawin));
        Guard.Against.Null(agama, nameof(agama));
        Guard.Against.Null(suku, nameof(suku));
        Guard.Against.Null(pekerjaanDk, nameof(pekerjaanDk));
        Guard.Against.Null(pendidikanDk, nameof(pendidikanDk));
        
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
    
    public PasienReff ToReff() => new PasienReff(PasienId, Person.PersonName, 
        Person.TglLahir, Person.Gender);
    
    #endregion
}
