using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature;

public interface IAmbulanceDal:
    IInsert<AmbulanceModel>,
    IUpdate<AmbulanceModel>,
    IGetData<AmbulanceModel, IAmbulanceKey>,
    IListData<AmbulanceModel>
{
    
}