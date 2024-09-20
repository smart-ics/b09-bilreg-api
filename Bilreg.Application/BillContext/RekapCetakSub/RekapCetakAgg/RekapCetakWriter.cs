using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BillContext.RekapCetakSub.RekapCetakAgg;

public interface IRekapCetakWriter : INunaWriterWithReturn<RekapCetakModel>
{
    
}

public class RekapCetakWriter : IRekapCetakWriter 
{
    private readonly IRekapCetakDal _rekapCetakDal;

    public RekapCetakWriter(IRekapCetakDal rekapCetakDal)
    {
        _rekapCetakDal = rekapCetakDal;
    }
    
    public RekapCetakModel Save(RekapCetakModel model)
    {
        var rekapCetakDb = _rekapCetakDal.GetData(model);
        if (rekapCetakDb is null)
            _rekapCetakDal.Insert(model);
        else
            _rekapCetakDal.Update(model);
        return model;
    }
}

