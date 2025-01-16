namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public partial class PasienModel(string pasienId)
    : IPasienKey
{
    public PasienModel(string pasienId, string pasienName) : this(pasienId)
    {
        PasienName = pasienName;
    }
    public string PasienId { get; protected set; } = pasienId;
    public string PasienName { get; protected set; } = string.Empty;
    public string NickName { get; protected set; }
    public string TempatLahir { get; protected set; }
    public DateTime TglLahir { get; protected set; }
    public string Gender { get; protected set; }
    public DateTime TglMedrec { get; protected set; }
    public string IbuKandung { get; protected set; }
    public string GolDarah { get; protected set; }

    public string StatusNikahId { get; protected set; }
    public string StatusNikahName { get; protected set; }
    public string AgamaId { get; protected set; }
    public string AgamaName { get; protected set; }
    public string SukuId { get; protected set; }
    public string SukuName { get; protected set; }
    public string PekerjaanDkId { get; protected set; }
    public string PekerjaanDkName { get; protected set; }
    public string PendidikanDkId { get; protected set; }
    public string PendidikanDkName { get; protected set; }

    public string Alamat { get; protected set; }
    public string Alamat2 { get; protected set; }
    public string Alamat3 { get; protected set; }
    public string Kota { get; protected set; }
    public string KodePos { get; protected set; }

    public string KelurahanId { get; protected set; }
    public string KelurahanName { get; protected set; }
    public string KecamatanName { get; protected set; }
    public string KabupatenName { get; protected set; }
    public string PropinsiName { get; protected set; }

    public string JenisId { get; protected set; }
    public string NomorId { get; protected set; }
    public string NomorKk { get; protected set; }

    public string Email { get; protected set; }
    public string NoTelp { get; protected set; }
    public string NoHp { get; protected set; }
    
    public string KeluargaName { get; protected set; }
    public string KeluargaRelasi { get; protected set; }
    public string KeluargaNoTelp { get; protected set; }
    public string KeluargaAlamat1 { get; protected set; }
    public string KeluargaAlamat2 { get; protected set; }
    public string KeluargaKota { get; protected set; }
    public string KeluargaKodePos { get; protected set; }
    
    public string NoMedrecInduk { get; protected set; }
    public string IsAktif { get; protected set; }

    public List<PasienLogModel> ListLog { get; protected set; } = [];
}