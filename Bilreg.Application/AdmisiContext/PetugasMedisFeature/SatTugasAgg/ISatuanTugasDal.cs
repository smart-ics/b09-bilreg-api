using Bilreg.Domain.AdmisiContext.PetugasMedisSub;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.SatTugasAgg
{
    public interface ISatuanTugasDal :
    //IInsert<SatuanTugasModel>,
    //IUpdate<SatuanTugasModel>,
    //IDelete<ISatuanTugasKey>,
    IGetData<SatTugasType, ISatTugasKey>,
    IListData<SatTugasType>
    {
    }

}
