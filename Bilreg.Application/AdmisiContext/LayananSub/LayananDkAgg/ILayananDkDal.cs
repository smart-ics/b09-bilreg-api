using Bilreg.Domain.AdmisiContext.LayananSub.LayananDkAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananDkAgg
{
    public interface ILayananDkDal :
        IGetData<LayananDkModel, ILayananDkKey>,
        IListData<LayananDkModel>
    {
    }
}
