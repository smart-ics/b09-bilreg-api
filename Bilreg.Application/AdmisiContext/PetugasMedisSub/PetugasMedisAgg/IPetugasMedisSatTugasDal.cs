using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public interface IPetugasMedisSatTugasDal :
    IInsertBulk<PetugasMedisSatTugasModel>,
    IDelete<IPetugasMedisKey>,
    IListData<PetugasMedisSatTugasModel, IPetugasMedisKey>
{
}