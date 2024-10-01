using Bilreg.Domain.BillContext.RoomChargeSub.TipeKamarAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BillContext.RoomChargeSub.TipeKamarAgg;

public interface ITipeKamarWriter : INunaWriterWithReturn<TipeKamarModel>
{
    public void Delete(ITipeKamarKey key);
}
public class TipeKamarWriter:ITipeKamarWriter
{
    private readonly ITipeKamarDal _tipeKamarDal;

    public TipeKamarWriter(ITipeKamarDal tipeKamarDal)
    {
        _tipeKamarDal = tipeKamarDal;
    }

    public TipeKamarModel Save(TipeKamarModel model)
    {
        var existingTipeKamar = _tipeKamarDal.GetData(model);
        if (existingTipeKamar is null)
            _tipeKamarDal.Insert(model);
        else
            _tipeKamarDal.Update(model);
        return model;
    }
    public void Delete(ITipeKamarKey Key)
    {
        _tipeKamarDal.Delete(Key);
    }
}