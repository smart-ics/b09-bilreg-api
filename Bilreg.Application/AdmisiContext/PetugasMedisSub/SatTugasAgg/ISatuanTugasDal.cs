using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.SatTugasAgg
{
    public interface ISatuanTugasDal :
    IInsert<SatuanTugasModel>,
    IUpdate<SatuanTugasModel>,
    IDelete<ISatuanTugasKey>,
    IGetData<SatuanTugasModel, ISatuanTugasKey>,
    IListData<SatuanTugasModel>
    {
    }

}
