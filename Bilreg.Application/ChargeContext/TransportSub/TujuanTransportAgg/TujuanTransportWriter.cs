using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BillContext.TransportSub.TujuanTransportAgg;

public interface ITujuanTransportWriter : INunaWriterWithReturn<TujuanTransportModel>
{
    public void Delete(ITujuanTransportKey key);
}
public class TujuanTransportWriter: ITujuanTransportWriter
{
    private readonly ITujuanTransportDal _tujuanTransportDal;

    public TujuanTransportWriter(ITujuanTransportDal tujuanTransportDal)
    {
        _tujuanTransportDal = tujuanTransportDal;
    }

    public TujuanTransportModel Save(TujuanTransportModel model)
    {
        var tujuanTransport = _tujuanTransportDal.GetData(model);
        if (tujuanTransport is not null) 
            _tujuanTransportDal.Update(model);
        else
            _tujuanTransportDal.Insert(model);
        return model;
    }

    public void Delete(ITujuanTransportKey key)
    {
        _tujuanTransportDal.Delete(key);
    }
}