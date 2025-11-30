using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature;

public interface ITujuanTransportDal:
    IInsert<TujuanTransportModel>,
    IUpdate<TujuanTransportModel>,
    IDelete<ITujuanTransportKey>,
    IGetData<TujuanTransportModel, ITujuanTransportKey>,
    IListData<TujuanTransportModel>
{
}