using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
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
    private readonly IPasien2Dal _pasien2Dal;
    private readonly IParamSistemDal _paramSistemDal;
    private readonly INunaCounterBL _counter;
    
    private const string KODE_RS_PARAM_KEY = "RS__XXXXXX_KODE";
    private const string NO_MR_PARAM_KEY = "NOMR";
    
    public PasienRepo(IPasienDal pasienDal, 
        IPasien2Dal pasien2Dal, 
        IParamSistemDal paramSistemDal, 
        INunaCounterBL counter)
    {
        _pasienDal = pasienDal;
        _pasien2Dal = pasien2Dal;
        _paramSistemDal = paramSistemDal;
        _counter = counter;
    }

    public Result<IPasienKey> SaveChanges(PasienModel model)
    {
        if (model.PasienId == "[NEW]")
            model.SetPasienId(NewPasienId());
        
        using var trans = TransHelper.NewScope();
        
        var pasienDb = _pasienDal.GetData(PasienModel.Key(model.PasienId));
        if (pasienDb is not null)
            _pasienDal.Update(new PasienDto(model));
        else
            _pasienDal.Insert(new PasienDto(model));
        
        var pasien2Db = _pasien2Dal.GetData(PasienModel.Key(model.PasienId));
        if (pasien2Db is not null)
            _pasien2Dal.Update(Pasien2Dto.Create(model));
        else
            _pasien2Dal.Insert(Pasien2Dto.Create(model));
        
        trans.Complete();
        return Result<IPasienKey>.Success(model);
    }
    
    

    public MayBe<PasienModel> LoadEntity(IPasienKey key)
    {
        var pasienDto = _pasienDal.GetData(key);
        if (pasienDto is null)
            return MayBe<PasienModel>.None;
        var pasien = pasienDto.ToModel();

        var pasien2 = _pasien2Dal.GetData(key);
        var alamatKtp = AlamatType.Default;
        if (pasien2 is not null)
            alamatKtp = pasien2.GetAlamatKtp();
        
        pasien.SetAdministrativeInfo(pasien.AlamatDomisili, alamatKtp,
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
    
    private string NewPasienId()
    {
        var kodeRsEncrypted = _paramSistemDal.GetData(KODE_RS_PARAM_KEY)?.Value ?? string.Empty;
        var kodeRs = X1EncryptionHelper.DecodingNeo(kodeRsEncrypted);
        var newId = _counter.GenerateDec(NO_MR_PARAM_KEY, kodeRs, 15, string.Empty);
        return newId;
    }

}