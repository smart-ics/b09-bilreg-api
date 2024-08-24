using Bilreg.Domain.PasienContext.StatusSosialSub.SukuAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.PasienContext.StatusSosialSub.SukuAgg;

public interface ISukuWriter : INunaWriterWithReturn<SukuModel>
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

    public SukuModel Save(SukuModel model)
    {
        var sukuDb = _sukuDal.GetData(model);
        if (sukuDb is null)
            _sukuDal.Insert(model);
        else
            _sukuDal.Update(model);

        return model;
    }

    public void Delete(ISukuKey sukuKey)
    {
        _sukuDal.Delete(sukuKey);
    }
}