using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;
using Nuna.Lib.DataAccessHelper;

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
