using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature;

public interface ITrsTransportRepo :
    ISaveChange<TrsTransportModel>,
    ILoadEntity<TrsTransportModel, ITrsTransportKey>,
    IDelete<ITrsTransportKey>
{
    MayBe<TrsTransportModel> LoadEntity(string trsTransportId);
    IEnumerable<TrsTransportModel> ListDataByReg(string regId);
    IEnumerable<TrsTransportModel> ListActiveDispatched();
    bool HasActiveDispatchForAmbulance(string ambulanceId, string? excludeTrsId = null);
    bool HasActiveDispatchForDriver(string driverId, string? excludeTrsId = null);
}
