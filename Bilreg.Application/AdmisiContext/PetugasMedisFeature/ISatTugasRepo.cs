using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PetugasMedisFeature;

public interface ISatTugasRepo :
    ISaveChange<SatTugasType>,
    ILoadEntity<SatTugasType, ISatTugasKey>,
    IDeleteEntity<ISatTugasKey>,
    IListData<SatTugasType>
{
}