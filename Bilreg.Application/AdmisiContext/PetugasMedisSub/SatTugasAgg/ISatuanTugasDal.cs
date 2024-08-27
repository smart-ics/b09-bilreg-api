using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
