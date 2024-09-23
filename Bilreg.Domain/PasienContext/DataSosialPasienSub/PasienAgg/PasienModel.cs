using System.Text.Json.Serialization;

namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public partial class PasienModel(string PasienId, string PasienName)
    : IPasienKey
{
    [JsonConstructor]
    public PasienModel(): this(string.Empty, string.Empty) {}

    [JsonInclude] public string PasienId { get; protected set; } = PasienId;
    [JsonInclude] public string PasienName { get; protected set; } = PasienName;
    [JsonInclude]
    public string NickName { get; protected set; } = string.Empty;

    [JsonInclude] public string TempatLahir { get; protected set; } = string.Empty;
    [JsonInclude]
    public DateTime TglLahir { get; protected set; }

    [JsonInclude] public string Gender { get; protected set; } = string.Empty;
    [JsonInclude]
    public DateTime TglMedrec { get; protected set; }
    [JsonInclude]
    public string IbuKandung { get; protected set; } = string.Empty;
    [JsonInclude]
    public string GolDarah { get; protected set; } = string.Empty;
    
    [JsonInclude]
    public string StatusNikahId { get; protected set; } = string.Empty;
    [JsonInclude]
    public string StatusNikahName { get; protected set; } = string.Empty;
    [JsonInclude]
    public string AgamaId { get; protected set; } = string.Empty;
    [JsonInclude]
    public string AgamaName { get; protected set; } = string.Empty;
    [JsonInclude]
    public string SukuId { get; protected set; } = string.Empty;
    [JsonInclude]
    public string SukuName { get; protected set; } = string.Empty;
    [JsonInclude]
    public string PekerjaanDkId { get; protected set; } = string.Empty;
    [JsonInclude]
    public string PekerjaanDkName { get; protected set; } = string.Empty;
    [JsonInclude]
    public string PendidikanDkId { get; protected set; } = string.Empty;
    [JsonInclude]
    public string PendidikanDkName { get; protected set; } = string.Empty;

    [JsonInclude]
    public string Alamat { get; protected set; } = string.Empty;
    [JsonInclude]
    public string Alamat2 { get; protected set; } = string.Empty;
    [JsonInclude]
    public string Alamat3 { get; protected set; } = string.Empty;
    [JsonInclude]
    public string Kota { get; protected set; } = string.Empty;
    [JsonInclude]
    public string KodePos { get; protected set; } = string.Empty;

    [JsonInclude]
    public string KelurahanId { get; protected set; } = string.Empty;
    [JsonInclude]
    public string KelurahanName { get; protected set; } = string.Empty;
    [JsonInclude]
    public string KecamatanName { get; protected set; } = string.Empty;
    [JsonInclude]
    public string KabupatenName { get; protected set; } = string.Empty;
    [JsonInclude]
    public string PropinsiName { get; protected set; } = string.Empty;

    [JsonInclude]
    public string JenisId { get; protected set; } = string.Empty;
    [JsonInclude]
    public string NomorId { get; protected set; } = string.Empty;
    [JsonInclude]
    public string NomorKk { get; protected set; } = string.Empty;

    [JsonInclude]
    public string Email { get; protected set; } = string.Empty;
    [JsonInclude]
    public string NoTelp { get; protected set; } = string.Empty;
    [JsonInclude]
    public string NoHp { get; protected set; } = string.Empty;

    [JsonInclude]
    public string KeluargaName { get; protected set; } = string.Empty;
    [JsonInclude]
    public string KeluargaRelasi { get; protected set; } = string.Empty;
    [JsonInclude]
    public string KeluargaNoTelp { get; protected set; } = string.Empty;
    [JsonInclude]
    public string KeluargaAlamat1 { get; protected set; } = string.Empty;
    [JsonInclude]
    public string KeluargaAlamat2 { get; protected set; } = string.Empty;
    [JsonInclude]
    public string KeluargaKota { get; protected set; } = string.Empty;
    [JsonInclude]
    public string KeluargaKodePos { get; protected set; } = string.Empty;
    
    [JsonInclude]
    public string NoMedrecInduk { get; protected set;}
    [JsonInclude]
    public string IsAktif { get; protected set;}
    
}