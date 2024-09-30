using Bilreg.Domain.BillContext.RoomChargeSub.BedAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BillContext.RoomChargeSub.BedAgg;

public interface IBedWriter : INunaWriterWithReturn<BedModel>
{
    public void Delete(IBedKey key);
}

public class BedWriter : IBedWriter
{
    private readonly IBedDal _bedDal;

    public BedWriter(IBedDal bedDal)
    {
        _bedDal = bedDal;
    }
    public BedModel Save(BedModel model)
    {
        var BedDb = _bedDal.GetData(model);
        if (BedDb is null)
            _bedDal.Insert(model);
        else
            _bedDal.Update(model);
        return model;
    }

    public void Delete(IBedKey key)
    {
        _bedDal.Delete(key);
    }
}

