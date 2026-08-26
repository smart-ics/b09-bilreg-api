using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.SalesContext.ReturJualFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.SalesContext.ReturJualFeature;

public interface IReturJualRepo :
    ISaveChange<ReturJualModel>,
    ILoadEntity<ReturJualModel, IReturJualKey>,
    IDeleteEntity<IReturJualKey>,
    IListData<ReturJualModel, IRegKey>
{
}
