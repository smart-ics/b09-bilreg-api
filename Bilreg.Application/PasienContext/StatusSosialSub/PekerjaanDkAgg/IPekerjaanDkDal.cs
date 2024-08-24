using Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.StatusSosialSub.PekerjaanDkAgg
{
    public interface IPekerjaanDkDal :
        IInsert<PekerjaanDkModel>,
        IUpdate<PekerjaanDkModel>,
        IDelete<IPekerjaanDkKey>,
        IGetData<PekerjaanDkModel, IPekerjaanDkKey>,
        IListData<PekerjaanDkModel>
    {
    }
}
