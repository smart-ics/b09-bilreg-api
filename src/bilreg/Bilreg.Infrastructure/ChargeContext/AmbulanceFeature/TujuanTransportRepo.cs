using Bilreg.Application.ChargeContext.AmbulanceFeature;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.AmbulanceFeature;

public class TujuanTransportRepo : ITujuanTransportRepo
{
    private readonly ITujuanTransportDal _tujuanTransportDal;

    public TujuanTransportRepo(ITujuanTransportDal tujuanTransportDal)
    {
        _tujuanTransportDal = tujuanTransportDal;
    }

    public void SaveChanges(TujuanTransportModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var existing = _tujuanTransportDal.GetData(model);
        if (existing is null)
        {
            _tujuanTransportDal.Insert(model);
        }
        else
        {
            _tujuanTransportDal.Update(model);
        }
    }

    public MayBe<TujuanTransportModel> LoadEntity(ITujuanTransportKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var model = _tujuanTransportDal.GetData(key);
        return model is null ? MayBe<TujuanTransportModel>.None : MayBe.From(model);
    }

    public MayBe<TujuanTransportModel> LoadEntity(string tujuanTransportId)
    {
        return LoadEntity(new TujuanTransportKey(tujuanTransportId));
    }

    public void Delete(ITujuanTransportKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        _tujuanTransportDal.Delete(key);
    }

    public IEnumerable<TujuanTransportModel> ListData()
    {
        return _tujuanTransportDal.ListData() ?? [];
    }
}
