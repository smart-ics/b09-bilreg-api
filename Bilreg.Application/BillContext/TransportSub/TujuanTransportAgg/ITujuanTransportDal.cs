using Bilreg.Domain.BillContext.TransportSub.TujuanTransportAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TransportSub.TujuanTransportAgg;

public interface ITujuanTransportDal:
    IInsert<TujuanTransportModel>,
    IUpdate<TujuanTransportModel>,
    IDelete<ITujuanTransportKey>,
    IGetData<TujuanTransportModel, ITujuanTransportKey>,
    IListData<TujuanTransportModel>
{
}