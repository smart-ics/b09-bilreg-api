using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PetugasMedisFeature.SatTugasAgg
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
