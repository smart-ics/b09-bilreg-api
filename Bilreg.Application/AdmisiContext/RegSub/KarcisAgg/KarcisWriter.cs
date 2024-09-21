using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;

public interface IKarcisWriter: INunaWriterWithReturn<KarcisModel>
{
}

public class KarcisWriter: IKarcisWriter
{
    private readonly IKarcisDal _karcisDal;
    private readonly IKarcisKomponenDal _karcisKomponenDal;
    private readonly IKarcisLayananDal _karcisLayananDal;

    public KarcisWriter(IKarcisDal karcisDal, IKarcisKomponenDal karcisKomponenDal, IKarcisLayananDal karcisLayananDal)
    {
        _karcisDal = karcisDal;
        _karcisKomponenDal = karcisKomponenDal;
        _karcisLayananDal = karcisLayananDal;
    }

    public KarcisModel Save(KarcisModel model)
    {
        model.SyncId();

        var karcisDb = _karcisDal.GetData(model);
        if (karcisDb is null) 
            _karcisDal.Insert(model);
        else 
            _karcisDal.Update(model);

        _karcisKomponenDal.Delete(model);
        _karcisLayananDal.Delete(model);
        
        _karcisKomponenDal.Insert(model.ListKomponen);
        _karcisLayananDal.Insert(model.ListLayanan);
        
        return model;
    }
}