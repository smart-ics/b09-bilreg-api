using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public interface IPetugasMedisDal :
    IInsert<PetugasMedisModel>,
    IUpdate<PetugasMedisModel>,
    IDelete<IPetugasMedisKey>,
    IGetData<PetugasMedisModel, IPetugasMedisKey>,
    IListData<PetugasMedisModel>

{
}