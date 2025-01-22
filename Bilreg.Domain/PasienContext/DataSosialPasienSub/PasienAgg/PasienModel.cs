using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.AgamaAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.PendidikanDkAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.StatusKawinDkAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.SukuAgg;

namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public partial class PasienModel
    : IPasienKey
{
    public PasienModel(string pasienId, string pasienName, DateTime tglLahir, string gender)
    {
        PasienId = pasienId;
        PasienName = pasienName;
        TglLahir = tglLahir;
        Gender = gender;

        Address = AddressObj.Default;
        StatusKawinDk = StatusKawinDkModel.Default;
        Agama = AgamaModel.Default;;
        Suku = SukuModel.Default;
        PekerjaanDk = PekerjaanDkModel.Default;
        PendidikanDk = PendidikanDkModel.Default;
    }
    public string PasienId { get; private set; } 
    public string PasienName { get; private set; }
    public DateTime TglLahir { get; private set; }
    public string Gender { get; private set; }
    public string NickName { get; private set; } = string.Empty;
    public string TempatLahir { get; private set; } = string.Empty;
    public DateTime TglMedrec { get; private set; } = DateTime.Now;
    public string IbuKandung { get; private set; } = string.Empty;
    public string GolDarah { get; private set; } = string.Empty;

    public StatusKawinDkModel StatusKawinDk { get; private set; }
    public AgamaModel Agama { get; private set; }
    public SukuModel Suku { get; private set; }
    public PekerjaanDkModel PekerjaanDk { get; private set; }
    public PendidikanDkModel PendidikanDk { get; private set; }

    public AddressObj Address { get; private set; } 
    public KelurahanModel Kelurahan { get; private set; }
    public IdentityObj Identity { get; private set; } 
    public ContactObj Contact { get; private set; }
    public KeluargaObj Keluarga { get; private set; }
    
    public string NoMedrecInduk { get; private set; }
    public bool IsAktif { get; private set; }
}

public record AddressObj(string Alamat, string Alamat2, string Alamat3, string Kota, string KodePos)
{
    public static AddressObj Default => new AddressObj(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
};

public record IdentityObj(string JenisId, string NomorId, string NomorKk);

public record ContactObj(string Email, string NoTelp, string NoHp);

public record KeluargaObj(string Name, string Relasi, ContactObj Contact, AddressObj Address);
