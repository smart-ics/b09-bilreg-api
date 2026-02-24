using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Domain.Shared.Param;
using Castle.Components.DictionaryAdapter.Xml;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public class PasienRepo : IPasienRepo
{
    private readonly IPasienDal _pasienDal;
    private readonly IPasienKtpDal _pasienKtpDal;
    private readonly IGetKodeRsService _getKodeRsSvc;
    private readonly IPasienIdDal _pasienIdDal;
    private readonly IPasienTelpDal _pasienTelpDal;
    public PasienRepo(IPasienDal pasienDal,
        IPasienKtpDal pasienKtpDal,
        IGetKodeRsService getKodeRsSvc,
        IPasienIdDal pasienIdDal,
        IPasienTelpDal pasienTelpDal)
    {
        _pasienDal = pasienDal;
        _pasienKtpDal = pasienKtpDal;
        _getKodeRsSvc = getKodeRsSvc;
        _pasienIdDal = pasienIdDal;
        _pasienTelpDal = pasienTelpDal;
    }

    public Result<IPasienKey> SaveChanges(PasienModel model)
    {
        using var trans = TransHelper.NewScope();
        
        var pasienDb = _pasienDal.GetData(PasienModel.Key(model.PasienId));
        if (pasienDb is not null)
            _pasienDal.Update(PasienDto.FromModel(model));
        else
            _pasienDal.Insert(PasienDto.FromModel(model));

        var pasienTelpDb = _pasienTelpDal.ListData(model)?.ToList() ?? [];
        var listTelp = pasienTelpDb.Where(x => x.fs_kd_jenis_telp == "HP")?.ToList() ?? [];
        if (listTelp.Count() > 0)
            _pasienTelpDal.Update(PasienTelpDto.FromMr(model), "HP");
        else
            _pasienTelpDal.Insert(PasienTelpDto.FromMr(model));

        var pasien2Db = _pasienKtpDal.GetData(PasienModel.Key(model.PasienId));
        if (pasien2Db is not null)
            _pasienKtpDal.Update(PasienKtpDto.FromModel(model));
        else
            _pasienKtpDal.Insert(PasienKtpDto.FromModel(model));
        
        var pasienIdDb = _pasienIdDal.GetData(model);
        if(pasienIdDb is not null)
            _pasienIdDal.Update(PasienIdDto.FromModel(model));
        else
            _pasienIdDal.Insert(PasienIdDto.FromModel(model));


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
        var ktpDto = _pasienKtpDal.GetData(key) ?? PasienKtpDto.Default;

        var alamatKtp = new AlamatType([ktpDto.fs_alm_ktp], "-", "-");
        var kelurahanKtp = new KelurahanType(ktpDto.fs_kd_kelurahan_ktp, ktpDto.fs_kelurahan_ktp,
            new KecamatanReff(ktpDto.fs_kd_kecamatan_ktp, ktpDto.fs_kecamatan_ktp),
            new KabupatenReff(ktpDto.fs_kd_kabupaten_ktp, ktpDto.fs_kabupaten_ktp),
            new PropinsiType(ktpDto.fs_kd_propinsi_ktp, ktpDto.fs_propinsi_ktp));
        var ktp = new KtpType(ktpDto.fs_nik, alamatKtp, ktpDto.fs_rt_ktp, ktpDto.fs_rw_ktp,
            kelurahanKtp);

        var propinsi = new PropinsiType(dto.fs_kd_propinsi, dto.fs_nm_propinsi);
        var kabupatenReff = new KabupatenReff(dto.fs_kd_kabupaten, dto.fs_nm_kabupaten);
        var kecamatanReff = new KecamatanReff(dto.fs_kd_kecamatan, dto.fs_nm_kecamatan);
        var kelurahan = string.IsNullOrWhiteSpace(dto.fs_kd_kelurahan)
            ? KelurahanType.Default
            : new KelurahanType(dto.fs_kd_kelurahan, dto.fs_nm_kelurahan, kecamatanReff, kabupatenReff, propinsi );


        var contactKeluarga = new ContactType(JenisContactEnum.Phone, dto.fs_telp_keluarga ?? "-");
        var almKeluarga = new AlamatType([dto.fs_alm1_keluarga, dto.fs_alm2_keluarga], dto.fs_kota_keluarga, dto.fs_kd_pos_keluarga);
        var pasienKeluarga = new PasienKeluargaType(dto.fs_nm_keluarga, dto.fs_hub_keluarga, contactKeluarga, almKeluarga);

        var agama = string.IsNullOrWhiteSpace(dto.fs_kd_agama)
            ? AgamaType.Default
            : new AgamaType(dto.fs_kd_agama, dto.fs_nm_agama);

        var suku = string.IsNullOrWhiteSpace(dto.fs_kd_suku)
            ? SukuType.Default
            : new SukuType(dto.fs_kd_suku, dto.fs_nm_suku);

        var statusKawin = string.IsNullOrWhiteSpace(dto.fs_kd_status_kawin_dk)
            ? StatusKawinDkType.Default
            : new StatusKawinDkType(dto.fs_kd_status_kawin_dk, dto.fs_nm_status_kawin_dk);
        
        var pendidikan = string.IsNullOrWhiteSpace(dto.fs_kd_pendidikan_dk) 
            ? PendidikanDkType.Default 
            : new PendidikanDkType(dto.fs_kd_pendidikan_dk, dto.fs_nm_pendidikan_dk);
        
        var pekerjaan = string.IsNullOrWhiteSpace(dto.fs_kd_pekerjaan_dk) 
            ? PekerjaanDkType.Default
            : new PekerjaanDkType(dto.fs_kd_pekerjaan_dk, dto.fs_nm_pekerjaan_dk);
        
        var phonePasien = new ContactType(JenisContactEnum.Mobile, dto.fs_no_hp);
        var email = new ContactType(JenisContactEnum.Email, dto.fs_email);
        var listContact = new List<ContactType>{ email, phonePasien };

        var pasien = new PasienModel(key.PasienId,
            person, dto.fs_nm_alias, dto.fs_temp_lahir, new GolDarahType(dto.fs_gol_darah),
            dto.fs_nm_ibu_kandung, 
            ktp, kelurahan, IdentitasType.Default, 
            listContact, pasienKeluarga, 
            agama, suku, statusKawin, 
            pendidikan, pekerjaan, 
            DateTime.MinValue, dto.fb_aktif);

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
        var listPasienByPasienId = new List<PasienPersonView>();
        var listPasienByTglLahir = new List<PasienPersonView>();
        var listPasienByName = new List<PasienPersonView>();

        if (pasienFinder.PasienId != string.Empty)
            listPasienByPasienId = ListPasienByPasienId(pasienFinder.PasienId);

        if (pasienFinder.TglLahir != string.Empty)
            listPasienByTglLahir =  ListPasienByTglLahir(pasienFinder.TglLahir);
        
        if (pasienFinder.StringVariants.Count > 0)
            listPasienByName = ListPasienByName(pasienFinder.StringVariants);

        var result = listPasienByPasienId
            .Union(listPasienByTglLahir)
            .Union(listPasienByName);

        return result;
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
    private List<PasienPersonView> ListPasienByTglLahir(string tglLahir)
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
                IdentitasType.Ktp(x.fs_kd_identitas)))).ToList();
        return result;
    }
    private List<PasienPersonView> ListPasienByName(Dictionary<string, string[]> stringVariants)
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
                IdentitasType.Ktp(x.fs_kd_identitas)))).ToList();
        return result;
    }
    #endregion


}