using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;

public interface IAmbulanceWriter : INunaWriterWithReturn<AmbulanceModel> {}

public class AmbulanceWriter: IAmbulanceWriter
{
    private readonly IAmbulanceDal _ambulanceDal;
    private readonly IAmbulanceKomponenDal _ambulanceKomponenDal;

    public AmbulanceWriter(IAmbulanceDal ambulanceDal, IAmbulanceKomponenDal ambulanceKomponenDal)
    {
        _ambulanceDal = ambulanceDal;
        _ambulanceKomponenDal = ambulanceKomponenDal;
    }

    public AmbulanceModel Save(AmbulanceModel model)
    {
        model.SyncId();

        var ambulanceDb = _ambulanceDal.GetData(model);
        if (ambulanceDb is null)
            _ambulanceDal.Insert(model);
        else 
            _ambulanceDal.Update(model);
        
        _ambulanceKomponenDal.Delete(model);
        _ambulanceKomponenDal.Insert(model.ListKomponen);

        return model;
    }
}