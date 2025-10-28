using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Infrastructure.Helpers;
using Bilreg.Infrastructure.ParamContext;
using FluentAssertions;
using Moq;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public class PasienRepo : IPasienRepo
{
    private readonly IPasienDal _pasienDal;
    private readonly IPasienKtpDal _pasien2Dal;
    
    public PasienRepo(IPasienDal pasienDal, 
        IPasienKtpDal pasien2Dal)
    {
        _pasienDal = pasienDal;
        _pasien2Dal = pasien2Dal;
    }

    public Result<IPasienKey> SaveChanges(PasienModel model)
    {
        using var trans = TransHelper.NewScope();
        
        var pasienDb = _pasienDal.GetData(PasienModel.Key(model.PasienId));
        if (pasienDb is not null)
            _pasienDal.Update(PasienDto.FromModel(model));
        else
            _pasienDal.Insert(PasienDto.FromModel(model));
        
        var pasien2Db = _pasien2Dal.GetData(PasienModel.Key(model.PasienId));
        if (pasien2Db is not null)
            _pasien2Dal.Update(PasienKtpDto.Create(model));
        else
            _pasien2Dal.Insert(PasienKtpDto.Create(model));
        
        trans.Complete();
        return Result<IPasienKey>.Success(model);
    }
    
    

    public MayBe<PasienModel> LoadEntity(IPasienKey key)
    {
        var dto = _pasienDal.GetData(key);
        if (dto is null)
            return MayBe<PasienModel>.None;
        
        var dtoKtp = _pasien2Dal.GetData(key);
        
        var alamat = new AlamatType(
            [dto.fs_alm_pasien, dto.fs_alm2_pasien, dto.fs_alm3_pasien], 
            dto.fs_kota_pasien, dto.fs_kd_pos_pasien);
        var contact = new ContactType(JenisContactEnum.Phone, dto.fs_tlp_pasien);
        var identitas = IdentitasType.Ktp(dto.fs_kd_identitas);
        var person = new PersonInfoType(dto.fs_nm_pasien, 
            DateOnly.Parse(dto.fd_tgl_lahir), dto.fs_jns_kelamin, 
            alamat, contact, identitas);
        var pasien = new PasienModel(key.PasienId,
            person, dto.fs_nm_alias, dto.fs_temp_lahir, new GolDarahType(dto.fs_gol_darah),
            dto.fs_nm_ibu_kandung, 
            AlamatType.Default, KelurahanType.Default, IdentitasType.Default, 
            new List<ContactType>(), PasienKeluargaType.Default, 
            AgamaType.Default, SukuType.Default, StatusKawinDkType.Default, 
            PendidikanDkType.Default, PekerjaanDkType.Default, 
            DateTime.MinValue, false);
        
        var pasien2 = _pasien2Dal.GetData(key);
        var alamatKtp = AlamatType.Default;
        if (pasien2 is not null)
            alamatKtp = pasien2.GetAlamatKtp();
        
        pasien.SetAdministrativeInfo(alamatKtp,
            pasien.Kelurahan, pasien.Identitas, pasien.KartuKeluarga, pasien.ListContact,
            pasien.PasienKeluarga);
        
        return MayBe.From(pasien);
    }

    public void DeleteEntity(IPasienKey key)
    {
        using var trans = TransHelper.NewScope();
        _pasienDal.Delete(PasienModel.Key(key.PasienId));
        _pasien2Dal.Delete(PasienModel.Key(key.PasienId));
        trans.Complete();
    }
    
    public IEnumerable<IPasienPersonalInfo> ListData(PasienFinder filter)
    {
        throw new NotImplementedException();
    }
}