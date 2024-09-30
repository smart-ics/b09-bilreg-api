using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public class PasienModelSerializable() : PasienModel(string.Empty)
{
    public new string PasienId { get => base.PasienId; set => base.PasienId = value; }
    public new string PasienName { get=> base.PasienName; set => base.PasienName = value; }    
    public new string NickName { get => base.NickName; set => base.NickName = value; }
    public new DateTime TglMedrec { get => base.TglMedrec; set =>base.TglMedrec = value; }    
    
    public new string TempatLahir {get => base.TempatLahir; set => base.TempatLahir = value;}
    public new DateTime TglLahir { get => base.TglLahir; set =>base.TglLahir = value; }   
    public new string IbuKandung {get => base.IbuKandung; set => base.IbuKandung = value;}    
    public new string GolDarah {get => base.IbuKandung; set => base.GolDarah = value;}  
    public new string Gender {get => base.IbuKandung; set => base.Gender = value;}    

    public new string StatusNikahId {get => base.IbuKandung; set => base.StatusNikahId = value;}    
    public new string StatusNikahName {get => base.IbuKandung; set => base.StatusNikahName = value;}    
    public new string AgamaId {get => base.IbuKandung; set => base.AgamaId = value;}    
    public new string AgamaName {get => base.IbuKandung; set => base.AgamaName = value;}    
    public new string SukuId {get => base.IbuKandung; set => base.SukuId = value;}    
    public new string SukuName {get => base.IbuKandung; set => base.SukuName = value;}    
    public new string PekerjaanDkId {get => base.IbuKandung; set => base.PekerjaanDkId = value;}    
    public new string PekerjaanDkName {get => base.IbuKandung; set => base.PekerjaanDkName = value;}    
    public new string PendidikanDkId {get => base.IbuKandung; set => base.PendidikanDkId = value;}    
    public new string PendidikanDkName {get => base.IbuKandung; set => base.PendidikanDkName = value;}  

    public new string Alamat {get => base.IbuKandung; set => base.Alamat = value;}    
    public new string Alamat2 {get => base.IbuKandung; set => base.Alamat2 = value;}    
    public new string Alamat3 {get => base.IbuKandung; set => base.Alamat3 = value;}    
    public new string Kota {get => base.IbuKandung; set => base.Kota = value;}    
    public new string KodePos {get => base.IbuKandung; set => base.KodePos = value;}  
    public new string KelurahanId {get => base.IbuKandung; set => base.KelurahanId = value;}    
    public new string KelurahanName {get => base.IbuKandung; set => base.KelurahanName = value;}    
    public new string KecamatanName {get => base.IbuKandung; set => base.KecamatanName = value;}    
    public new string KabupatenName {get => base.IbuKandung; set => base.KabupatenName = value;}    
    public new string PropinsiName {get => base.IbuKandung; set => base.PropinsiName = value;}  

    public new string JenisId {get => base.IbuKandung; set => base.JenisId = value;}    
    public new string NomorId {get => base.IbuKandung; set => base.NomorId = value;}    
    public new string NomorKk {get => base.IbuKandung; set => base.NomorKk = value;}  

    public new string Email {get => base.IbuKandung; set => base.Email = value;}    
    public new string NoTelp {get => base.IbuKandung; set => base.NoTelp = value;}    
    public new string NoHp {get => base.IbuKandung; set => base.NoHp = value;}  

    public new string KeluargaName {get => base.IbuKandung; set => base.KeluargaName = value;}    
    public new string KeluargaRelasi {get => base.IbuKandung; set => base.KeluargaRelasi = value;}    
    public new string KeluargaNoTelp {get => base.IbuKandung; set => base.KeluargaNoTelp = value;}    
    public new string KeluargaAlamat1 {get => base.IbuKandung; set => base.KeluargaAlamat1 = value;}    
    public new string KeluargaAlamat2 {get => base.IbuKandung; set => base.KeluargaAlamat2 = value;}    
    public new string KeluargaKota {get => base.IbuKandung; set => base.KeluargaKota = value;}    
    public new string KeluargaKodePos {get => base.IbuKandung; set => base.KeluargaKodePos = value;}  
    
    public new string NoMedrecInduk {get => base.IbuKandung; set => base.NoMedrecInduk = value;}
    public new bool IsAktif { get; set; } = false;
}