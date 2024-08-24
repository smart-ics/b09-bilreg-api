using Bilreg.Domain.PasienContext.StatusSosialSub.AgamaAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.StatusSosialSub.AgamaAgg
{
    public interface IAgamaDal : 
        IInsert<AgamaModel>,
        IUpdate<AgamaModel>,
        IDelete<IAgamaKey>,
        IGetData<AgamaModel, IAgamaKey>,
        IListData<AgamaModel>
    {
    }
}
