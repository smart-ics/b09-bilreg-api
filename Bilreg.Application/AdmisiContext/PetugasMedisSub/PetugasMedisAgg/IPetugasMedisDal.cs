using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public interface IPetugasMedisDal :
    IInsert<PetugasMedisType>,
    IUpdate<PetugasMedisType>,
    IDelete<IPetugasMedisKey>,
    IGetDataMayBe<PetugasMedisType, IPetugasMedisKey>,
    IListDataMayBe<PetugasMedisType>

{
}