using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IAntrianMapRepo :
    ISaveChange<AntrianMapModel>,
    ILoadEntity<AntrianMapModel, IAntrianMapKey>
{
}
