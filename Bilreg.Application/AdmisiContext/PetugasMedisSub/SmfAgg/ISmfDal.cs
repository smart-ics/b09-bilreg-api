using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.SmfAgg
{
    public interface ISmfDal :
        IInsert<SmfModel>,
        IUpdate<SmfModel>,
        IDelete<ISmfKey>,
        IGetData<SmfModel, ISmfKey>,
        IListData<SmfModel> 
    {
    }

}
