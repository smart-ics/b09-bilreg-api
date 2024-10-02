using Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;

public interface ITipeTarifWriter : INunaWriterWithReturn<TipeTarifModel>
{
    public void Delete(ITipeTarifKey key);
}

public class TipeTarifWriter : ITipeTarifWriter
{
    private readonly ITipeTarifDal _tarifDal;

    public TipeTarifWriter(ITipeTarifDal tarifDal)
    {
        _tarifDal = tarifDal;
    }
    
    public TipeTarifModel Save(TipeTarifModel model)
    {
        var TipeTarifDB = _tarifDal.GetData(model);
        if (TipeTarifDB is null)
            _tarifDal.Insert(model);
        else 
            _tarifDal.Update(model);
        return model;
    }

    public void Delete(ITipeTarifKey key)
    {
        _tarifDal.Delete(key);
    }
}