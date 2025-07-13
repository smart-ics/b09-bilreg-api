using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentValidation;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public interface IPasienWriter : INunaWriterWithReturn<PasienModel>
{
}

public class PasienWriter : IPasienWriter
{
    private readonly IPasienDal _pasienDal;

    public PasienWriter(IPasienDal pasienDal)
    {
        _pasienDal = pasienDal;
    }

    public PasienModel Save(PasienModel model)
    {
        var pasienDb = _pasienDal.GetData2(model);
        if (pasienDb.IsExist)
            _pasienDal.Insert(model);
        else
            _pasienDal.Update(model);

        return model;
    }
}