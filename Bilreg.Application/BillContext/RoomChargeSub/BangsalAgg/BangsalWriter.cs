using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BillContext.RoomChargeSub.BangsalAgg;

public interface IBangsalWriter : INunaWriterWithReturn<BangsalModel>
{
    public void Delete(IBangsalKey Key);
}

public class BangsalWriter : IBangsalWriter
{
    private readonly IBangsalDal _bangsalDal;

    public BangsalWriter(IBangsalDal bangsalDal)
    {
        _bangsalDal = bangsalDal;
    }
    public BangsalModel Save(BangsalModel model)
    {
        var bangsalDb = _bangsalDal.GetData(model);
        if (bangsalDb is null)
            _bangsalDal.Insert(model);
        else 
            _bangsalDal.Update(model);
        return model;
    }

    public void Delete(IBangsalKey Key)
    {
        _bangsalDal.Delete(Key);
    }
}