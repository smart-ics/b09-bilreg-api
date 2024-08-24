using Bilreg.Domain.PasienContext.StatusSosialSub.StatusKawinDkAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.StatusSosialSub.StatusKawinDkAgg
{
    public interface IStatusKawinDkDal :
                IInsert<StatusKawinDkModel>,
                IUpdate<StatusKawinDkModel>,
                IDelete<IStatusKawinDkKey>,
                IGetData<StatusKawinDkModel, IStatusKawinDkKey>,
                IListData<StatusKawinDkModel>

    {

    }
}
