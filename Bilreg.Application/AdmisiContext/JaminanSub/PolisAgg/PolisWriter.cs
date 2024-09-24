using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using FluentValidation;
using Nuna.Lib.CleanArchHelper;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.AdmisiContext.JaminanSub.PolisAgg;

public  interface IPolisWriter : INunaWriterWithReturn<PolisModel>
{
}

public class PolisWriter : IPolisWriter
{
    private readonly IPolisDal _polisDal;
    private readonly IPolisCoverDal _polisCoverDal;
    private readonly IValidator<PolisModel> _validator;

    public PolisWriter(IPolisDal polisDal, 
        IPolisCoverDal polisCoverDal, 
        IValidator<PolisModel> validator)
    {
        _polisDal = polisDal;
        _polisCoverDal = polisCoverDal;
        _validator = validator;
    }

    public PolisModel Save(PolisModel model)
    {
        var valResult = _validator.Validate(model);
        if (!valResult.IsValid)
            throw new ValidationException(valResult.Errors);
        
        var db = _polisDal.GetData(model);

        using var trans = TransHelper.NewScope();
        
        if (db is null)
            _polisDal.Insert(model);
        else
            _polisDal.Update(model);

        _polisCoverDal.Delete(model);
        _polisCoverDal.Insert(model.ListCover);
        trans.Complete();
        return model;
    }
}