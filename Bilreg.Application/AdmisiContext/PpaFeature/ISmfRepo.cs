using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PpaFeature;

public interface ISmfRepo :
    ISaveChange<SmfType>,
    ILoadEntity<SmfType, ISmfKey>,
    IDeleteEntity<ISmfKey>,
    IListData<SmfType>
{
}