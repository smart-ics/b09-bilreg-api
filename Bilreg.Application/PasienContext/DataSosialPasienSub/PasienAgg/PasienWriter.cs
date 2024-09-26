using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using FluentValidation;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public interface IPasienWriter : INunaWriterWithReturn<PasienModel>
{
}

public class PasienWriter : IPasienWriter
{
    private readonly IPasienDal _pasienDal;
    private readonly IValidator<PasienModel> _validator;
    private readonly IPasienLogDal _pasienLogDal;

    public PasienWriter(IPasienDal pasienDal, IValidator<PasienModel> validator, IPasienLogDal pasienLogDal)
    {
        _pasienDal = pasienDal;
        _validator = validator;
        _pasienLogDal = pasienLogDal;
    }

    public PasienModel Save(PasienModel model)
    {
        var validateResult = _validator.Validate(model);
        if (!validateResult.IsValid)
            throw new ValidationException(validateResult.Errors);
        
        var pasienDb = _pasienDal.GetData(model);
        if (pasienDb is null)
            _pasienDal.Insert(model);
        else
            _pasienDal.Update(model);
        
        model.SyncId();
        _pasienLogDal.Delete(model);
        _pasienLogDal.Insert(model.ListLog);

        return model;
    }
}