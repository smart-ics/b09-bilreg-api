using Bilreg.Application.ChargeContext.AmbulanceFeature;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.AmbulanceFeature;

public class AmbulanceRepo : IAmbulanceRepo
{
    private readonly IAmbulanceDal _ambulanceDal;
    private readonly IAmbulanceKomponenDal _ambulanceKomponenDal;
    private readonly ITransportTujuanDal _transportTujuanDal;

    public AmbulanceRepo(
        IAmbulanceDal ambulanceDal,
        IAmbulanceKomponenDal ambulanceKomponenDal,
        ITransportTujuanDal transportTujuanDal)
    {
        _ambulanceDal = ambulanceDal;
        _ambulanceKomponenDal = ambulanceKomponenDal;
        _transportTujuanDal = transportTujuanDal;
    }

    public void SaveChanges(AmbulanceModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.SyncId();

        var existing = _ambulanceDal.GetData(model);
        if (existing is null)
        {
            _ambulanceDal.Insert(model);
        }
        else
        {
            _ambulanceDal.Update(model);
        }

        _ambulanceKomponenDal.Delete(model);
        if (model.ListKomponen.Count > 0)
        {
            _ambulanceKomponenDal.Insert(model.ListKomponen);
        }

        _transportTujuanDal.Delete(model);
        if (model.ListTujuan.Count > 0)
        {
            _transportTujuanDal.Insert(model.ListTujuan);
        }
    }

    public MayBe<AmbulanceModel> LoadEntity(IAmbulanceKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var model = _ambulanceDal.GetData(key);
        if (model is null)
            return MayBe<AmbulanceModel>.None;

        var listKomponen = _ambulanceKomponenDal.ListData(key)?.ToList() ?? [];
        if (listKomponen.Count > 0)
        {
            model.Attach(listKomponen);
        }

        var listTujuan = _transportTujuanDal.ListData(key)?.ToList() ?? [];
        if (listTujuan.Count > 0)
        {
            model.AttachTujuan(listTujuan);
        }

        return MayBe.From(model);
    }

    public MayBe<AmbulanceModel> LoadEntity(string ambulanceId)
    {
        return LoadEntity(new AmbulanceKey(ambulanceId));
    }

    public void Delete(IAmbulanceKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        _transportTujuanDal.Delete(key);
        _ambulanceKomponenDal.Delete(key);
        _ambulanceDal.Delete(key);
    }

    public IEnumerable<AmbulanceModel> ListData()
    {
        var list = _ambulanceDal.ListData()?.ToList() ?? [];
        foreach (var model in list)
        {
            var listKomponen = _ambulanceKomponenDal.ListData(model)?.ToList() ?? [];
            if (listKomponen.Count > 0) model.Attach(listKomponen);

            var listTujuan = _transportTujuanDal.ListData(model)?.ToList() ?? [];
            if (listTujuan.Count > 0) model.AttachTujuan(listTujuan);
        }
        return list;
    }
}
