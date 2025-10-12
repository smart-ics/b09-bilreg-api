using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IServicePointDal :
    IInsert<ServicePointType>,
    IUpdate<ServicePointType>,
    IDelete<ServicePointType>,
    IGetDataMayBe<ServicePointType, IServicePointKey>
{
}