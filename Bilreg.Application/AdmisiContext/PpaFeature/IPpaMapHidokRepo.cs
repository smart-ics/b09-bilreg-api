using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PpaFeature;

public interface IPpaMapHidokRepo :
    ISaveChange<PpaMapHidokType>,
    ILoadEntity<PpaMapHidokType, IPpaMapHidokKey>,
    IDeleteEntity<IPpaMapHidokKey>,
    IListData<PpaMapHidokType>
{
}