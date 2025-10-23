using Bilreg.Domain.AdmisiContext.LayananSub;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananAgg;

public interface ILayananDal :
    //IInsert<LayananModel>,
    //IUpdate<LayananModel>,
    //IDelete<ILayananKey>,
    IGetDataMayBe<LayananType, ILayananKey>,
    IListDataMayBe<LayananType>
{
}