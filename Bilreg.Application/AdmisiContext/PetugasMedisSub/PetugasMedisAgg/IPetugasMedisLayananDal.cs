using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public interface IPetugasMedisLayananDal :
    IInsertBulk<PetugasMedisLayananType>,
    IDelete<IPetugasMedisKey>,
    IListDataMayBe<PetugasMedisLayananType, IPetugasMedisKey>
{
}