using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;

public interface IAmbulanceKomponenDal:
    IInsertBulk<AmbulanceKomponenModel>,
    IDelete<IAmbulanceKey>,
    IListData<AmbulanceKomponenModel, IAmbulanceKey>
{
    
}