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
    
    public PasienRepo(IPasienDal pasienDal, 
        IPasien2Dal pasien2Dal)
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
        // var pasienDto = _pasienDal.GetData(key);
        // if (pasienDto is null)
        //     return MayBe<PasienModel>.None;
        // var pasien = pasienDto.ToModel();
        //
        // var pasien2 = _pasien2Dal.GetData(key);
        // var alamatKtp = AlamatType.Default;
        // if (pasien2 is not null)
        //     alamatKtp = pasien2.GetAlamatKtp();
        //
        // pasien.SetAdministrativeInfo(alamatKtp,
        //     pasien.Kelurahan, pasien.Identitas, pasien.KartuKeluarga, pasien.ListContact,
        //     pasien.PasienKeluarga);
        //
        // return MayBe.From(pasien);
        throw new NotImplementedException();
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