using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public interface IPetugasMedisSatTugasDal :
    IInsertBulk<PetugasMedisSatTugasType>,
    IDelete<IPetugasMedisKey>,
    IListData<PetugasMedisSatTugasType, IPetugasMedisKey>
{
}