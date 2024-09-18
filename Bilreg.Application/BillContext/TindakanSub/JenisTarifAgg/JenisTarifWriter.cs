using Bilreg.Domain.BillContext.TindakanSub.JenisTarifAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BillContext.TindakanSub.JenisTarifAgg;

public interface IJenisTarifWriter : INunaWriterWithReturn<JenisTarifModel>
{
    public void Delete (IJenisTarifKey key);
}

public class JenisTarifWriter : IJenisTarifWriter
{
    private readonly IJenisTarifDal _jenisTarifDal;

    public JenisTarifWriter(IJenisTarifDal jenisTarifDal)
    {
        _jenisTarifDal = jenisTarifDal;
    }
    
    public JenisTarifModel Save(JenisTarifModel model)
    {
        var jenisTarifDB = _jenisTarifDal.GetData(model);
        if (jenisTarifDB is null)
            _jenisTarifDal.Insert(model);
        else
        {
            _jenisTarifDal.Update(model);
        }
        return model;
    }

    public void Delete(IJenisTarifKey key)
    {
        _jenisTarifDal.Delete(key);
    }
}