using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;

// ReSharper disable InconsistentNaming
namespace Bilreg.Infrastructure.AdmisiContext.LayananFeature;

public record LayananDto(
    string fs_kd_layanan,
    string fs_nm_layanan,
    bool fb_aktif,
    string fs_kd_instalasi,
    string fs_kd_layanan_dk,
    string fs_kd_layanan_tipe_dk,
    string fs_kd_unit,
    string fs_kd_layanan_bpjs,
    string fs_nm_instalasi,
    string fs_kd_instalasi_dk,
    string fs_nm_layanan_dk,
    string fs_nm_layanan_tipe_dk,
    string fs_nm_instalasi_dk,
    string fs_nm_unit,
    string fs_nm_layanan_bpjs)
{
    public static LayananDto FromModel(LayananType model)
        => new(
            model.LayananId,
            model.LayananName,
            model.IsAktif,
            model.Instalasi.InstalasiId,
            model.LayananDk.LayananDkId,
            model.TipeLayananDk.TipeLayananDkId,
            model.UnitPcc.UnitId,
            model.PoliBpjs.PoliBpjsId,
            model.Instalasi.InstalasiName,
            model.InstalasiDk.InstalasiDkId,
            model.LayananDk.LayananDkName,
            model.TipeLayananDk.TipeLayananDkName,
            model.InstalasiDk.InstalasiDkName,
            model.UnitPcc.UnitName,
            model.PoliBpjs.PoliBpjsName);

    public LayananType ToModel()
    {
        var instalasi = new InstalasiReff(fs_kd_instalasi, fs_nm_instalasi);
        var layananDk = new LayananDkReff(fs_kd_layanan_dk, fs_nm_layanan_dk);
   
        var tipeLayananDk = fs_kd_layanan_tipe_dk.Trim().Length == 0 ? new TipeLayananDkType("-", "-") 
            : new TipeLayananDkType(fs_kd_layanan_tipe_dk, fs_nm_layanan_tipe_dk);
   
        var instalasiDk = new InstalasiDkType(fs_kd_instalasi_dk, fs_nm_instalasi_dk);
        var unitPcc = new UnitReff(fs_kd_unit, fs_nm_unit);
        
        var poliBpjs = fs_kd_layanan_bpjs.Trim().Length == 0 ? new PoliBpjsReff("-", "-")
            : new PoliBpjsReff(fs_kd_layanan_bpjs, fs_nm_layanan_bpjs);
        
        return new LayananType(fs_kd_layanan, fs_nm_layanan, fb_aktif, 
            instalasi, layananDk, tipeLayananDk, instalasiDk, unitPcc, poliBpjs);
    }
}