using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature;

public interface ITujuanTransportRepo :
    ISaveChange<TujuanTransportModel>,
    ILoadEntity<TujuanTransportModel, ITujuanTransportKey>,
    IDelete<ITujuanTransportKey>,
    IListData<TujuanTransportModel>
{
    MayBe<TujuanTransportModel> LoadEntity(string tujuanTransportId);
}
