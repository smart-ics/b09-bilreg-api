using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PetugasMedisFeature;

public interface ISmfRepo :
    ISaveChange<SmfType>,
    ILoadEntity<SmfType, ISmfKey>,
    IDeleteEntity<ISmfKey>,
    IListData<SmfType>
{
}