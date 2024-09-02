using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananAgg;

public interface ILayananDal :
    IInsert<LayananModel>,
    IUpdate<LayananModel>,
    IDelete<ILayananKey>,
    IGetData<LayananModel, ILayananKey>,
    IListData<LayananModel>
{
}