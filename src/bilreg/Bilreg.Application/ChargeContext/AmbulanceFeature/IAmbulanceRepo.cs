using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature;

public interface IAmbulanceRepo :
    ISaveChange<AmbulanceModel>,
    ILoadEntity<AmbulanceModel, IAmbulanceKey>,
    IDelete<IAmbulanceKey>,
    IListData<AmbulanceModel>
{
    MayBe<AmbulanceModel> LoadEntity(string ambulanceId);
}
