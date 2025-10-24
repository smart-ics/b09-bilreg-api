using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.PasienContext.PasienFeature;

public class PasienModel : IPasienKey, IPasienPersonalInfo, IPasienAdministrativeInfo, IPasienStatusSosial
{
    private readonly List<ContactType> _listContact;
    public PasienModel(
        string pasienId, string pasienName, DateTime tglLahir, string gender, 
        string nickName, string tempatLahir, string ibuKandung, GolDarahType golDarah, 
        AlamatType alamatDomisili, AlamatType alamatKtp, KelurahanType kelurahan, 
        IdentitasType identitas, IdentitasType kartuKeluarga, 
        IEnumerable<ContactType> listContact, PasienKeluargaType keluarga, 
        StatusKawinDkType statusKawinDk, AgamaType agama, SukuType suku, 
        PekerjaanDkType pekerjaanDk, PendidikanDkType pendidikanDk, 
        DateTime tglMedRec, bool isAktif)
    {
        PasienId = pasienId;
        PasienName = pasienName;
        TglLahir = tglLahir;
        Gender = gender;
        NickName = nickName;
        TempatLahir = tempatLahir;
        IbuKandung = ibuKandung;
        GolDarah = golDarah;
        
        AlamatDomisili = alamatDomisili;
        AlamatKtp = alamatKtp;
        Kelurahan = kelurahan;
        
        Identitas = identitas;
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

    #region PROPERTIES
    //      personal info
    //          MANDATORY PROPERTIES
    public string PasienId { get; private set; } 
    public string PasienName { get; init; }
    public DateTime TglLahir { get; init; }
    public string Gender { get; init; }
    //
    public string NickName { get; private set; }
    public string TempatLahir { get; private set; }
    public string IbuKandung { get; private set; } 
    public GolDarahType GolDarah { get; private set; } 
    
    //      administrative info
    public AlamatType AlamatKtp { get; private set; }
    public AlamatType AlamatDomisili { get; private set; }
    public KelurahanType Kelurahan { get; private set; } 
    public IdentitasType Identitas { get; private set; }
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

    public void SetPersonalInfo(string nickName, string tempatLahir, string ibuKandung,
        GolDarahType golDarah)
    {
        Guard.Against.NullOrWhiteSpace(nickName, nameof(nickName));
        Guard.Against.NullOrWhiteSpace(tempatLahir, nameof(tempatLahir));
        Guard.Against.NullOrWhiteSpace(ibuKandung, nameof(ibuKandung));
        
        NickName = nickName;
        TempatLahir = tempatLahir;
        IbuKandung = ibuKandung;
        GolDarah = golDarah;
    }

    public void SetAdministrativeInfo(AlamatType alamatDomisili, AlamatType alamatKtp,
        KelurahanType kelurahan, IdentitasType identitas, IdentitasType kartuKeluarga, 
        IEnumerable<ContactType> listContact, PasienKeluargaType keluarga)
    {
        Guard.Against.Null(alamatDomisili, nameof(alamatDomisili));
        Guard.Against.Null(alamatKtp, nameof(alamatKtp));
        Guard.Against.Null(kelurahan, nameof(kelurahan));
        Guard.Against.Null(identitas, nameof(identitas));
        Guard.Against.Null(kartuKeluarga, nameof(kartuKeluarga));
        Guard.Against.Null(keluarga, nameof(keluarga));
        
        var listContactFetched = listContact.ToList();
        Guard.Against.Null(listContactFetched, nameof(listContact));

        if (kartuKeluarga.JenisId != "KK")
            throw new ArgumentException("Jenis Kartu Keluarga harus KK");

        AlamatDomisili = alamatDomisili; 
        AlamatKtp = alamatKtp;
        Kelurahan = kelurahan;
        Identitas = identitas;
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
    
    public PasienReff ToReff() => new PasienReff(PasienId, PasienName, TglLahir, Gender);
    
    public void SetPasienId(string id) => PasienId = id;
    
    #endregion
    
    #region STATIC FACTORY METHOD

    private const string NEW_ID = "[NEW]";
    public static PasienModel CreateNew(string pasienName,
        DateTime tglLahir, string gender)
    {
        Guard.Against.NullOrWhiteSpace(pasienName, nameof(pasienName));
        Guard.Against.Null(gender, nameof(gender));
        return new PasienModel(NEW_ID, pasienName, tglLahir, gender,
            "-", "-", "-", GolDarahType.Default, AlamatType.Default, AlamatType.Default,
            KelurahanType.Default, IdentitasType.Default, IdentitasType.Default,
            [], PasienKeluargaType.Default, StatusKawinDkType.Default, 
            AgamaType.Default, SukuType.Default, PekerjaanDkType.Default, PendidikanDkType.Default, 
            DateTime.Now, true);
    }
    public static PasienModel Default => CreateNew("-", new DateTime (3000,1,1), "-");
    public static IPasienKey Key(string id)
    {
        var result = CreateNew("-", new DateTime (3000,1,1), "-");
        result.SetPasienId(id);
        return result;        
    } 
    #endregion

}

public class PasienModelTest
{
    [Fact]
    public void UT1_Given8DigitsPatientId_WhenFormatted_ThenReturnShortId()
    {
        const string pasienId = "340407012345678";
        var pasien = PasienModel.CreateNew("Pasien", new DateTime(2000, 1, 1), "-");
        var shortId = pasien.GetNomorMedrec();
        shortId.Should().Be("12-34-56-78");
    }
}