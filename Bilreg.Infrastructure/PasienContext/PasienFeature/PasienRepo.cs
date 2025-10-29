using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public class PasienRepo : IPasienRepo
{
    private readonly IPasienDal _pasienDal;
    private readonly IPasienKtpDal _pasienKtpDal;
    private readonly IGetKodeRsService _getKodeRsSvc;
    
    public PasienRepo(IPasienDal pasienDal, 
        IPasienKtpDal pasienKtpDal, 
        IGetKodeRsService getKodeRsSvc)
    {
        _pasienDal = pasienDal;
        _pasienKtpDal = pasienKtpDal;
        _getKodeRsSvc = getKodeRsSvc;
    }

    public Result<IPasienKey> SaveChanges(PasienModel model)
    {
        using var trans = TransHelper.NewScope();
        
        var pasienDb = _pasienDal.GetData(PasienModel.Key(model.PasienId));
        if (pasienDb is not null)
            _pasienDal.Update(PasienDto.FromModel(model));
        else
            _pasienDal.Insert(PasienDto.FromModel(model));
        
        var pasien2Db = _pasienKtpDal.GetData(PasienModel.Key(model.PasienId));
        if (pasien2Db is not null)
            _pasienKtpDal.Update(PasienKtpDto.FromModel(model));
        else
            _pasienKtpDal.Insert(PasienKtpDto.FromModel(model));
        
        trans.Complete();
        return Result<IPasienKey>.Success(model);
    }

    public MayBe<PasienModel> LoadEntity(IPasienKey key)
    {
        var dto = _pasienDal.GetData(key);
        if (dto is null)
            return MayBe<PasienModel>.None;

        //  fetch pasien-dto 
        var alamat = new AlamatType(
            [dto.fs_alm_pasien, dto.fs_alm2_pasien, dto.fs_alm3_pasien],
            dto.fs_kota_pasien, dto.fs_kd_pos_pasien);
        var contact = new ContactType(JenisContactEnum.Phone, dto.fs_tlp_pasien);
        var identitas = IdentitasType.Ktp(dto.fs_kd_identitas);
        var person = new PersonInfoType(dto.fs_nm_pasien,
            DateOnly.Parse(dto.fd_tgl_lahir), dto.fs_jns_kelamin,
            alamat, contact, identitas);

        //  fetch ktp-dto
        var ktpDto = _pasienKtpDal.GetData(key);
        var alamatKtp = new AlamatType([ktpDto.fs_alm_ktp], "-", "-");
        var kelurahanKtp = new KelurahanType(ktpDto.fs_kd_kelurahan_ktp, ktpDto.fs_kelurahan_ktp,
            new KecamatanReff(ktpDto.fs_kd_kecamatan_ktp, ktpDto.fs_kecamatan_ktp),
            new KabupatenReff(ktpDto.fs_kd_kabupaten_ktp, ktpDto.fs_kabupaten_ktp),
            new PropinsiType(ktpDto.fs_kd_propinsi_ktp, ktpDto.fs_propinsi_ktp));
        var ktp = new KtpType(ktpDto.fs_nik, alamatKtp, ktpDto.fs_rt_ktp, ktpDto.fs_rw_ktp,
            kelurahanKtp);

        var pasien = new PasienModel(key.PasienId,
            person, dto.fs_nm_alias, dto.fs_temp_lahir, new GolDarahType(dto.fs_gol_darah),
            dto.fs_nm_ibu_kandung, 
            ktp, KelurahanType.Default, IdentitasType.Default, 
            new List<ContactType>(), PasienKeluargaType.Default, 
            AgamaType.Default, SukuType.Default, StatusKawinDkType.Default, 
            PendidikanDkType.Default, PekerjaanDkType.Default, 
            DateTime.MinValue, false);

        return MayBe.From(pasien);
    }

    public void DeleteEntity(IPasienKey key)
    {
        using var trans = TransHelper.NewScope();
        _pasienDal.Delete(PasienModel.Key(key.PasienId));
        _pasienKtpDal.Delete(PasienModel.Key(key.PasienId));
        trans.Complete();
    }
    
    public IEnumerable<PasienPersonView> ListData(string keyword)
    {
        var kodeRs = _getKodeRsSvc.Execute();
        var pasienFinder = PasienFinder.CreateNew(keyword, kodeRs);
        if (pasienFinder.PasienId != string.Empty)
            return ListPasienByPasienId(pasienFinder.PasienId);
        
        if (pasienFinder.TglLahir != string.Empty)
            return ListPasienByTglLahir(pasienFinder.TglLahir);
        
        if (pasienFinder.StringVariants.Count > 0)
            return ListPasienByName(pasienFinder.StringVariants);
        
        return new List<PasienPersonView>();
    }
    
    #region PRIVATE HELPER
    private List<PasienPersonView> ListPasienByPasienId(string pasienFinderPasienId)
    {
        var pasienDb = LoadEntity(PasienModel.Key(pasienFinderPasienId));
        if (!pasienDb.HasValue)
            return [];
        
        var result = new PasienPersonView(
            pasienDb.Value.PasienId,
            pasienDb.Value.Person);
        return [result];
    }
    private IEnumerable<PasienPersonView> ListPasienByTglLahir(string tglLahir)
    {
        var pasienDb = _pasienDal.ListData(tglLahir.ToDate(DateFormatEnum.YMD));
        if (pasienDb is null)
            return [];
        
        var result = pasienDb.Select(x => new PasienPersonView(
            x.fs_mr,
            new PersonInfoType(
                x.fs_nm_pasien,
                DateOnly.Parse(x.fd_tgl_lahir),
                x.fs_jns_kelamin,
                new AlamatType(
                    [x.fs_alm_pasien, x.fs_alm2_pasien, x.fs_alm3_pasien],
                    x.fs_kota_pasien, x.fs_kd_pos_pasien),
                new ContactType(JenisContactEnum.Phone, x.fs_tlp_pasien),
                IdentitasType.Ktp(x.fs_kd_identitas))));
        return result;
    }
    private IEnumerable<PasienPersonView> ListPasienByName(Dictionary<string, string[]> stringVariants)
    {
        var pasienDb = _pasienDal.ListDataByName(stringVariants);
        if (pasienDb is null)
            return [];
        
        var result = pasienDb.Select(x => new PasienPersonView(
            x.fs_mr,
            new PersonInfoType(
                x.fs_nm_pasien,
                DateOnly.Parse(x.fd_tgl_lahir),
                x.fs_jns_kelamin,
                new AlamatType(
                    [x.fs_alm_pasien, x.fs_alm2_pasien, x.fs_alm3_pasien],
                    x.fs_kota_pasien, x.fs_kd_pos_pasien),
                new ContactType(JenisContactEnum.Phone, x.fs_tlp_pasien),
                IdentitasType.Ktp(x.fs_kd_identitas))));
        return result;
    }
    #endregion


}