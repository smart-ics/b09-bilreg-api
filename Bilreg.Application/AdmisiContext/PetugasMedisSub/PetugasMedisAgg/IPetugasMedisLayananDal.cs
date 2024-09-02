using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public interface IPetugasMedisLayananDal :
    IInsertBulk<PetugasMedisLayananModel>,
    IDelete<IPetugasMedisKey>,
    IListData<PetugasMedisLayananModel, IPetugasMedisKey>
{
}