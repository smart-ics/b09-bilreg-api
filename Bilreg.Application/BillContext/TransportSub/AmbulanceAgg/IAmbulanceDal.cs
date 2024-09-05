using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;

public interface IAmbulanceDal:
    IInsert<AmbulanceModel>,
    IUpdate<AmbulanceModel>,
    IGetData<AmbulanceModel, IAmbulanceKey>,
    IListData<AmbulanceModel>
{
    
}