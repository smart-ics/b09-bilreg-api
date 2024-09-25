using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public class PasienModelSerializable() : PasienModel(string.Empty, string.Empty)
{
    public string PasienId { get; set; } = string.Empty;
    public string PasienName { get; set; } = string.Empty;    
    public string NickName { get; set; } = string.Empty;  
   
    public string TempatLahir { get; set; } = string.Empty;    
    public DateTime TglLahir { get; set; }   
    public string Gender { get; set; } = string.Empty;    
    public DateTime TglMedrec { get; set; }    
    public string IbuKandung { get; set; } = string.Empty;    
    public string GolDarah { get; set; } = string.Empty;  
      
    public string StatusNikahId { get; set; } = string.Empty;    
    public string StatusNikahName { get; set; } = string.Empty;    
    public string AgamaId { get; set; } = string.Empty;    
    public string AgamaName { get; set; } = string.Empty;    
    public string SukuId { get; set; } = string.Empty;    
    public string SukuName { get; set; } = string.Empty;    
    public string PekerjaanDkId { get; set; } = string.Empty;    
    public string PekerjaanDkName { get; set; } = string.Empty;    
    public string PendidikanDkId { get; set; } = string.Empty;    
    public string PendidikanDkName { get; set; } = string.Empty;  
    
    public string Alamat { get; set; } = string.Empty;    
    public string Alamat2 { get; set; } = string.Empty;    
    public string Alamat3 { get; set; } = string.Empty;    
    public string Kota { get; set; } = string.Empty;    
    public string KodePos { get; set; } = string.Empty;  
    
    public string KelurahanId { get; set; } = string.Empty;    
    public string KelurahanName { get; set; } = string.Empty;    
    public string KecamatanName { get; set; } = string.Empty;    
    public string KabupatenName { get; set; } = string.Empty;    
    public string PropinsiName { get; set; } = string.Empty;  
    
    public string JenisId { get; set; } = string.Empty;    
    public string NomorId { get; set; } = string.Empty;    
    public string NomorKk { get; set; } = string.Empty;  
    
    public string Email { get; set; } = string.Empty;    
    public string NoTelp { get; set; } = string.Empty;    
    public string NoHp { get; set; } = string.Empty;  
    
    public string KeluargaName { get; set; } = string.Empty;    
    public string KeluargaRelasi { get; set; } = string.Empty;    
    public string KeluargaNoTelp { get; set; } = string.Empty;    
    public string KeluargaAlamat1 { get; set; } = string.Empty;    
    public string KeluargaAlamat2 { get; set; } = string.Empty;    
    public string KeluargaKota { get; set; } = string.Empty;    
    public string KeluargaKodePos { get; set; } = string.Empty;  
      
    public string NoMedrecInduk { get; set; } = string.Empty;    
    public string IsAktif { get; set; } = string.Empty;  
}