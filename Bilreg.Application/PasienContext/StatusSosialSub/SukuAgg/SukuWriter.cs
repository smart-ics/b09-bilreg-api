using Bilreg.Domain.PasienContext.SukuFeature;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.PasienContext.StatusSosialSub.SukuAgg;

public interface ISukuWriter : INunaWriterWithReturn<SukuType>
{
    void Delete(ISukuKey sukuKey);
}

public class SukuWriter : ISukuWriter
{
    private readonly ISukuDal _sukuDal;

    public SukuWriter(ISukuDal sukuDal)
    {
        _sukuDal = sukuDal;
    }

    public SukuType Save(SukuType type)
    {
        var sukuDb = _sukuDal.GetData2(type);
        if (sukuDb.IsExist)
            _sukuDal.Insert(type);
        else
            _sukuDal.Update(type);

        return type;
    }

    public void Delete(ISukuKey sukuKey)
    {
        _sukuDal.Delete(sukuKey);
    }
}