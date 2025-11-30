using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature;

public interface IAmbulanceKomponenDal:
    IInsertBulk<AmbulanceKomponenModel>,
    IDelete<IAmbulanceKey>,
    IListData<AmbulanceKomponenModel, IAmbulanceKey>
{
    
}