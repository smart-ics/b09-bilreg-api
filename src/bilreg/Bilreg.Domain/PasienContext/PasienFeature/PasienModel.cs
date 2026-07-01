using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using static System.Net.WebRequestMethods;

namespace Bilreg.Domain.PasienContext.PasienFeature;

public class PasienModel : IPasienKey
{
    private readonly List<ContactType> _listContact;
    
    public PasienModel(string pasienId, PersonInfoType person,  
        string nickName, string tempatLahir, GolDarahType golDarah, string namaIbuKandung, 
        KtpType ktp, KelurahanType kelurahan, 
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
        
        Ktp = ktp;
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
        KtpType.Default, KelurahanType.Default, IdentitasType.Default, 
        new List<ContactType>(), PasienKeluargaType.Default, 
        AgamaType.Default, SukuType.Default, StatusKawinDkType.Default, 
        PendidikanDkType.Default, PekerjaanDkType.Default, 
        DateTime.MinValue, false);
    
    public static IPasienKey Key(string id) => new PasienModel(id,
        PersonInfoType.Default, "-", "-", GolDarahType.Default, "-", 
        KtpType.Default,KelurahanType.Default, IdentitasType.Default, 
        new List<ContactType>(), PasienKeluargaType.Default, 
        AgamaType.Default, SukuType.Default, StatusKawinDkType.Default, 
        PendidikanDkType.Default, PekerjaanDkType.Default, 
        DateTime.MinValue, false);



    #region PROPERTIES
    //      personal info
    public string PasienId { get; private set; } 
    public PersonInfoType Person { get; private set; }
    public string NickName { get; private set; }
    public string TempatLahir { get; private set; }
    public GolDarahType GolDarah { get; private set; }
    public string NamaIbuKandung { get; private set; }
    
    //      administrative info
    public KtpType Ktp { get; private set; }
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

    public string GetUmur()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (Person.TglLahir > today)
            return "0 tahun, 0 bulan, 0 hari";

        int tahun = today.Year - Person.TglLahir.Year;
        int bulan = today.Month - Person.TglLahir.Month;
        int hari = today.Day - Person.TglLahir.Day;

        if (hari < 0)
        {
            bulan--;
            var prevMonth = today.AddMonths(-1);
            hari += DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
        }

        if (bulan < 0)
        {
            tahun--;
            bulan += 12;
        }

        return $"{tahun} tahun, {bulan} bulan, {hari} hari";
    }

    public void UpdateAdminInfo(KelurahanType kelurahan, 
        IdentitasType kartuKeluarga, ContactType email, ContactType noHp,
        PasienKeluargaType pasienKeluarga, string namaIbuKandung,
        PersonInfoType person)
    {
        kelurahan ??= KelurahanType.Default;
        kartuKeluarga ??= IdentitasType.Default;
        email ??= ContactType.Default;
        noHp ??= ContactType.Default;
        pasienKeluarga ??= PasienKeluargaType.Default;
        namaIbuKandung ??= "-";
        var ktp = Ktp;
        var amalatKtp = ktp.Alamat with { Kota = person.Alamat.Kota };
        ktp = ktp with { Alamat = amalatKtp };


        Person = person;
        Kelurahan = kelurahan;
        KartuKeluarga = kartuKeluarga;
        PasienKeluarga = pasienKeluarga;
        NamaIbuKandung = namaIbuKandung;
        Ktp = ktp;
        if (email is not null)
        {
            _listContact.RemoveAll(x => x.JenisContact == JenisContactEnum.Email);
            _listContact.Add(email);
        }

        if (noHp is not null)
        {
            _listContact.RemoveAll(x => x.JenisContact == JenisContactEnum.Mobile);
            _listContact.Add(noHp);
        }
    }
    public void SetPersonInfo(PersonInfoType person, GolDarahType golDarah, string tempatLahir)
    {
        Guard.Against.Null(person, nameof(person));
        Guard.Against.NullOrWhiteSpace(tempatLahir, nameof(tempatLahir));
        Person = person;
        GolDarah = golDarah;
        TempatLahir = tempatLahir;
    }
    
    public void SetDataKtp(KtpType ktp)
    {
        Guard.Against.Null(ktp, nameof(ktp));
        var identity = IdentitasType.CreateNew("KTP", ktp.Nik);

        Ktp = ktp;
        Person.SetIdentity(identity);
    }

    public void SyncFromKtp(string pasienName, DateOnly tglLahir,
        string tempatLahir, string gender, string golDarah)
    {
        Person = new PersonInfoType(pasienName, tglLahir, gender, Person.Alamat, Person.Contact, Person.Identity);
        GolDarah = new GolDarahType(golDarah);
        TempatLahir = tempatLahir;
    }
    public void AddContact(ContactType contact)
    {
        Guard.Against.Null(contact, nameof(contact));
        _listContact.Add(contact);
    }
    public void SetAdministrativeInfo(KtpType ktp,
        KelurahanType kelurahan, IdentitasType kartuKeluarga, 
        IEnumerable<ContactType> listContact, PasienKeluargaType keluarga)
    {
        Guard.Against.Null(ktp, nameof(ktp));
        Guard.Against.Null(kelurahan, nameof(kelurahan));
        Guard.Against.Null(kartuKeluarga, nameof(kartuKeluarga));
        Guard.Against.Null(keluarga, nameof(keluarga));
        
        var listContactFetched = listContact.ToList();
        Guard.Against.Null(listContactFetched, nameof(listContact));

        if (kartuKeluarga.JenisId != "KK")
            throw new ArgumentException("Jenis Kartu Keluarga harus KK");

        Ktp = ktp;
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

    public void NonActive()
    {
        IsAktif = false;
    }
    public void ReActive()
    {
        IsAktif = true; 
    }
    public PasienReff ToReff() => new PasienReff(PasienId, Person.PersonName, 
        Person.TglLahir, Person.Gender);
    
    #endregion
}
