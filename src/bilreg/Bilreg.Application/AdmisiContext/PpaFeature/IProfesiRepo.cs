using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PpaFeature;

public interface IProfesiRepo :
    ISaveChange<ProfesiType>,
    ILoadEntity<ProfesiType, IProfesiKey>,
    IDeleteEntity<IProfesiKey>,
    IListData<ProfesiType>
{
}