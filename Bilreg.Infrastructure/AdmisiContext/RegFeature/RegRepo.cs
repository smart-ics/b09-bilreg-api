using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Domain.Shared.Param;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public class RegRepo : IRegRepo
{
    private readonly IRegDal _regDal;
    private readonly IRegJaminanDal _regJaminanDal;
    private readonly IRegKomponenDal _regKomponenDal;
    private readonly IGetKodeRsService _getKodeRsSvc;

    public RegRepo(IRegDal regDal, 
        IRegJaminanDal regJaminanDal, 
        IRegKomponenDal regKomponenDal,
        IGetKodeRsService getKodeRsService)
    {
        _regDal = regDal;
        _regJaminanDal = regJaminanDal;
        _regKomponenDal = regKomponenDal;
        _getKodeRsSvc = getKodeRsService;
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
        var dokter = new PpaReff(regDto.fs_kd_medis, regDto.fs_nm_medis);
        var layanan = new LayananReff(regDto.fs_kd_layanan, regDto.fs_nm_layanan);
        var karcis = new KarcisReff(regDto.fs_kd_karcis, regDto.fs_nm_karcis);
        // polis
        var regJmnDto = _regJaminanDal.GetData(key) ?? new RegJaminanDto("-", "-", "-", "-");
        var polis = new PolisReff(regJmnDto.fs_kd_polis, regJmnDto.fs_no_polis,
            regJmnDto.fs_atas_nama);

        //  komponen
        var regJaminanDto = _regJaminanDal.GetData(key) ?? new RegJaminanDto("-", "-", "-", "-");
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
            new PpaReff(x.fs_kd_medis, x.fs_nm_medis)));
        return listView;
    }

    public IEnumerable<RegSearchRegView> ListData(string keyword)
    {
        var kodeRs = _getKodeRsSvc.Execute();
        var pasienFinder = PasienFinder.CreateNew(keyword, kodeRs);
        var listRegByRegId = new List<RegSearchRegView>();
        var listRegByBookingId = new List<RegSearchRegView>();
        var listRegByPasienId = new List<RegSearchRegView>();
        var listRegByTglMasuk = new List<RegSearchRegView>();
        var listRegByName = new List<RegSearchRegView>();

        if (pasienFinder.RegId != string.Empty)
            listRegByRegId = ListRegByRegId(pasienFinder.RegId);

        if (pasienFinder.BookingId != string.Empty)
            listRegByBookingId = ListRegByBookingId(pasienFinder.BookingId);

        if (pasienFinder.PasienId != string.Empty)
            listRegByPasienId = ListRegByPasienId(pasienFinder.PasienId);

        if (pasienFinder.TglMasuk != string.Empty)
            listRegByTglMasuk = ListRegByTglMasuk(pasienFinder.TglMasuk);

        if (pasienFinder.StringVariants.Count > 0)
            listRegByName = ListRegByName(pasienFinder.StringVariants);

        var result = listRegByRegId
            .Union(listRegByBookingId)
            .Union(listRegByPasienId)
            .Union(listRegByTglMasuk)
            .Union(listRegByName);

        return result;
    }

    #region PRIVATE-HELPER
    private List<RegSearchRegView> ListRegByRegId(string regId)
    {
        var regDb = LoadEntity(RegModel.Key(regId));
        if (!regDb.HasValue)
            return [];
        
        var result = new RegSearchRegView(
            regDb.Value.RegId,
            regDb.Value.RegDate.ToString("yyyy-MM-dd"),
            regDb.Value.Pasien.PasienId,
            regDb.Value.Pasien.PasienName,
            regDb.Value.TipeJaminan.TipeJaminanName,
            regDb.Value.Layanan.LayananName,
            ((int)regDb.Value.JenisReg).ToString(),
            regDb.Value.JenisReg.ToString());
        return [result];
    }
    private List<RegSearchRegView> ListRegByBookingId(string bookingId)
    {
        var regDb = _regDal.GetData(BookingModel.Key(bookingId));
        if (regDb is null)
            return [];

        var result = new RegSearchRegView(
            regDb.fs_kd_reg,
            regDb.fd_tgl_masuk,
            regDb.fs_mr,
            regDb.fs_nm_pasien,
            regDb.fs_nm_tipe_jaminan,
            regDb.fs_nm_layanan,
            ((int)regDb.fs_kd_jenis_reg.ToJenisRegEnum()).ToString(),
            regDb.fs_kd_jenis_reg.ToJenisRegEnum().ToString());
        return [result];
    }
    private List<RegSearchRegView> ListRegByPasienId(string pasienId)
    {
        var listRegDb = _regDal.ListData(PasienModel.Key(pasienId));
        if (listRegDb is null)
            return [];
        
        var result = listRegDb.Select(x => new RegSearchRegView(
            x.fs_kd_reg,
            x.fd_tgl_masuk,
            x.fs_mr,
            x.fs_nm_pasien,
            x.fs_nm_tipe_jaminan,
            x.fs_nm_layanan,
            ((int)x.fs_kd_jenis_reg.ToJenisRegEnum()).ToString(),
            x.fs_kd_jenis_reg.ToJenisRegEnum().ToString()));
        return result.ToList();
    }
    private List<RegSearchRegView> ListRegByTglMasuk(string tglMasuk)
    {
        var listRegDb = _regDal.ListData(tglMasuk.ToDate(DateFormatEnum.YMD));
        if (listRegDb is null)
            return [];
        
        var result = listRegDb.Select(x => new RegSearchRegView(
            x.fs_kd_reg,
            x.fd_tgl_masuk,
            x.fs_mr,
            x.fs_nm_pasien,
            x.fs_nm_tipe_jaminan,
            x.fs_nm_layanan,
            ((int)x.fs_kd_jenis_reg.ToJenisRegEnum()).ToString(),
            x.fs_kd_jenis_reg.ToJenisRegEnum().ToString()));
        return result.ToList();
    }
    private List<RegSearchRegView> ListRegByName(Dictionary<string, string[]> stringVariants)
    {
        var listRegDb = _regDal.ListDataByName(stringVariants);
        if (listRegDb is null)
            return [];
        
        var result = listRegDb.Select(x => new RegSearchRegView(
            x.fs_kd_reg,
            x.fd_tgl_masuk,
            x.fs_mr,
            x.fs_nm_pasien,
            x.fs_nm_tipe_jaminan,
            x.fs_nm_layanan,
            ((int)x.fs_kd_jenis_reg.ToJenisRegEnum()).ToString(),
            x.fs_kd_jenis_reg.ToJenisRegEnum().ToString()));
        return result.ToList();
    }
    #endregion
}