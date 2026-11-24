using Bilreg.Application.ChargeContext.AmbulanceFeature;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.AmbulanceFeature;

public class TrsTransportRepo : ITrsTransportRepo
{
    private readonly ITrsTransportDal _trsTransportDal;
    private readonly ITrsTransportItemDal _trsTransportItemDal;

    public TrsTransportRepo(
        ITrsTransportDal trsTransportDal,
        ITrsTransportItemDal trsTransportItemDal)
    {
        _trsTransportDal = trsTransportDal;
        _trsTransportItemDal = trsTransportItemDal;
    }

    public void SaveChanges(TrsTransportModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var existing = _trsTransportDal.GetData(model);
        if (existing is null)
        {
            _trsTransportDal.Insert(model);
        }
        else
        {
            _trsTransportDal.Update(model);
        }

        _trsTransportItemDal.Delete(model);
        if (model.ListItems.Count > 0)
        {
            _trsTransportItemDal.Insert(model.ListItems);
        }
    }

    public MayBe<TrsTransportModel> LoadEntity(ITrsTransportKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var dto = _trsTransportDal.GetData(key);
        if (dto is null)
            return MayBe<TrsTransportModel>.None;

        var items = _trsTransportItemDal.ListData(key)?.ToList() ?? [];
        var model = dto.ToModel(items);
        return MayBe.From(model);
    }

    public MayBe<TrsTransportModel> LoadEntity(string trsTransportId)
    {
        return LoadEntity(new TrsTransportKey(trsTransportId));
    }

    public void Delete(ITrsTransportKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        _trsTransportItemDal.Delete(key);
        _trsTransportDal.Delete(key);
    }

    public IEnumerable<TrsTransportModel> ListDataByReg(string regId)
    {
        var dtoList = _trsTransportDal.ListDataByReg(regId)?.ToList() ?? [];
        var result = new List<TrsTransportModel>();

        foreach (var dto in dtoList)
        {
            var key = new TrsTransportKey(dto.fs_kd_trs);
            var items = _trsTransportItemDal.ListData(key)?.ToList() ?? [];
            result.Add(dto.ToModel(items));
        }

        return result;
    }

    public IEnumerable<TrsTransportModel> ListActiveDispatched()
    {
        var dtoList = _trsTransportDal.ListActiveDispatched()?.ToList() ?? [];
        var result = new List<TrsTransportModel>();

        foreach (var dto in dtoList)
        {
            var key = new TrsTransportKey(dto.fs_kd_trs);
            var items = _trsTransportItemDal.ListData(key)?.ToList() ?? [];
            result.Add(dto.ToModel(items));
        }

        return result;
    }

    public bool HasActiveDispatchForAmbulance(string ambulanceId, string? excludeTrsId = null)
    {
        if (string.IsNullOrWhiteSpace(ambulanceId)) return false;
        return _trsTransportDal.HasActiveDispatchForAmbulance(ambulanceId, excludeTrsId);
    }

    public bool HasActiveDispatchForDriver(string driverId, string? excludeTrsId = null)
    {
        if (string.IsNullOrWhiteSpace(driverId)) return false;
        return _trsTransportDal.HasActiveDispatchForDriver(driverId, excludeTrsId);
    }
}
