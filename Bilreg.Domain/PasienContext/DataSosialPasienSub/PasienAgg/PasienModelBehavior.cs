using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;

namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public partial class PasienModel
{
    public void SetPersonalInfo(
        string tempatLahir, DateTime tglLahir,
        string nickName, string gender, 
        string ibuKandung, string golDarah)
    {
        TempatLahir = tempatLahir;
        TglLahir = tglLahir;
        NickName = nickName;
        Gender = gender;
        IbuKandung = ibuKandung;
        GolDarah = golDarah;
    }

    public void SetTglMedrec(DateTime tgl)
    {
        TglMedrec = tgl;
    }
    
    public void SetAddress(string alamat, string alamat2, string alamat3,
        string kota, string kodePos)
    {
        Alamat = alamat;
        Alamat2 = alamat2;
        Alamat3 = alamat3;
        Kota = kota;
        KodePos = kodePos;
    }

    public void SetKelurahan(KelurahanModel kelurahan)
    {
        KelurahanId = kelurahan.KelurahanId;
        KelurahanName = kelurahan.KelurahanName;
        KecamatanName = kelurahan.KecamatanName;
        KabupatenName = kelurahan.KabupatenName;
        PropinsiName = kelurahan.PropinsiName;
    }

    public void SetIdentitas(string jenisId, string nomorId, string nomorKk)
    {
        JenisId = jenisId;
        NomorId = nomorId;
        NomorKk = nomorKk;
    }
    
    public void SetContact(string email, string noTelp, string noHp)
    {
        Email = email;
        NoTelp = noTelp;
        NoHp = noHp;
    }

    public void SetStatusSosial(string statusNikahId, string statusNikahName,
        string agamaId, string agamaName, string sukuId, string sukuName,
        string pekerjaanDkId, string pekerjaanDkName,
        string pendidikanDkId, string pendidikanDkName)
    {
        StatusNikahId = statusNikahId;
        StatusNikahName = statusNikahName;
        AgamaId = agamaId;
        AgamaName = agamaName;
        SukuId = sukuId;
        SukuName = sukuName;
        PekerjaanDkId = pekerjaanDkId;
        PekerjaanDkName = pekerjaanDkName;
        PendidikanDkId = pendidikanDkId;
        PendidikanDkName = pendidikanDkName;
    }
    
    public void SetKeluarga(string name, string relasi, string noTelp, string alamat1, string alamat2,
        string kota, string kodePos)
    {
        KeluargaName = name;
        KeluargaRelasi = relasi;
        KeluargaNoTelp = noTelp;
        KeluargaAlamat1 = alamat1;
        KeluargaAlamat2 = alamat2;
        KeluargaKota = kota;
        KeluargaKodePos = kodePos;
    }

    public string GetNoMedrec()
    {
        if (PasienId.Length < 15)
            return string.Empty;
        var lastEight = PasienId[^8..];
        return $"{lastEight[..2]}-{lastEight.Substring(2, 2)}-{lastEight.Substring(4, 2)}-{lastEight.Substring(6, 2)}";
    }
}
