using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BillContext.BedUsageFeature;
using Bilreg.Domain.Helpers.CommonValueObjects;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public class RegRepo : IRegRepo
{
    private readonly IRegDal _regDal;
    private readonly IRegJaminanDal _regJaminanDal;
    private readonly IRegKomponenDal _regKomponenDal;

    public RegRepo(IRegDal regDal, 
        IRegJaminanDal regJaminanDal, 
        IRegKomponenDal regKomponenDal)
    {
        _regDal = regDal;
        _regJaminanDal = regJaminanDal;
        _regKomponenDal = regKomponenDal;
    }

    public void SaveChanges(RegModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _regDal.Update(RegDto.FromModel(model)),
                onNone: () => _regDal.Insert(RegDto.FromModel(model))
            );
        
        _regJaminanDal.Delete(model);
        _regJaminanDal.Insert(RegJaminanDto.FromModel(model));
        
        var listKomponen = _regKomponenDal.ListData(model)?.ToList() ?? [];
        _regKomponenDal.Delete(model);
        _regKomponenDal.Insert(listKomponen);
    }

    public void Delete(IRegKey key)
    {
        _regDal.Delete(key);
        _regJaminanDal.Delete(key);
        _regKomponenDal.Delete(key);
    }

    public MayBe<RegModel> LoadEntity(IRegKey key)
    {
        var regDto = _regDal.GetData(key);
        if (regDto is null)
            return MayBe<RegModel>.None;
        
        var regMasukAudit = new AuditInfoType(regDto.fs_kd_petugas, regDto.fd_tgl_masuk, regDto.fs_jam_masuk);
        var regKeluarAudit = new AuditInfoType(regDto.fs_kd_petugas_keluar, regDto.fd_tgl_keluar, regDto.fs_jam_keluar);
        var regCancelOutAudit = new AuditInfoType(regDto.fs_kd_petugas_cancel_out, regDto.fd_tgl_cancel_out, regDto.fs_jam_cancel_out);
        var jenisReg = regDto.fs_kd_jenis_reg.ToJenisRegEnum();
        var pasien = new PasienReff(regDto.fs_mr, regDto.fs_nm_pasien, DateOnly.Parse(regDto.fd_tgl_lahir), regDto.fs_jns_kelamin);
        var tipeJmn = new TipeJaminanReff(regDto.fs_kd_tipe_jaminan, regDto.fs_nm_tipe_jaminan);
        var kelas = new KelasReff(regDto.fs_kd_kelas, regDto.fs_nm_kelas);
        var caraMasukDk = new CaraMasukDkType(regDto.fs_kd_cara_masuk_dk, regDto.fs_nm_cara_masuk_dk);
        var rujukan = new RujukanReff(regDto.fs_kd_rujukan, regDto.fs_nm_rujukan);
        var dokter = new PetugasMedisReff(regDto.fs_kd_medis, regDto.fs_nm_medis);
        var layanan = new LayananReff(regDto.fs_kd_layanan, regDto.fs_nm_layanan);
        var karcis = new KarcisReff(regDto.fs_kd_karcis, regDto.fs_nm_karcis);
        // polis
        var regJmnDto = _regJaminanDal.GetData(key);
        var polis = new PolisReff(regJmnDto.fs_kd_polis, regJmnDto.fs_no_polis,
            regJmnDto.fs_atas_nama);
        //  komponen
        var regJaminanDto = _regJaminanDal.GetData(key);
        var listKomponenDto = _regKomponenDal.ListData(key)?.ToList() ?? [];
        //  main object
        var result = new RegModel(
            regDto.fs_kd_reg, DateOnly.Parse(regDto.fd_tgl_masuk),
            regMasukAudit, regKeluarAudit, regCancelOutAudit, jenisReg,
            pasien, tipeJmn, polis, kelas, caraMasukDk, rujukan, dokter,
            layanan, karcis, listKomponenDto.Select(x => x.ToModel()));
        return MayBe.From(result);
    }
    public IEnumerable<RegView> ListData(Periode filter, ILayananKey layanan)
    {
        var listDto = _regDal.ListData(filter, layanan);
        var listView = listDto.Select(x => new RegView(
            x.fs_kd_reg, 
            x.fd_tgl_masuk,
            new PasienReff(x.fs_mr, x.fs_nm_pasien, DateOnly.Parse(x.fd_tgl_lahir), x.fs_jns_kelamin),
            new LayananReff(x.fs_kd_layanan, x.fs_nm_layanan),
            new PetugasMedisReff(x.fs_kd_medis, x.fs_nm_medis)));
        return listView;
    }
}