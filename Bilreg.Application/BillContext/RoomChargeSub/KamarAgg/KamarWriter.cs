using Bilreg.Domain.BillContext.RoomChargeSub.KamarAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BillContext.RoomChargeSub.KamarAgg;

public interface IKamarWriter: INunaWriterWithReturn<KamarModel>
{
    public void Delete(IKamarKey key);

}
public class KamarWriter : IKamarWriter
{
    private readonly IKamarDal _kamarDal;

    public KamarWriter(IKamarDal kamarDal)
    {
        _kamarDal = kamarDal;
    }

    public KamarModel Save(KamarModel model)
    {
        var existingKamar = _kamarDal.GetData(model);
        if (existingKamar is null)
            _kamarDal.Insert(model);
        else
            _kamarDal.Update(model);
        return model;
    }
    public void Delete(IKamarKey Key)
    {
        _kamarDal.Delete(Key);
    }
}