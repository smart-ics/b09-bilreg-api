using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IAntrianMapHdrRepo :
    ISaveChange<AntrianMapHdrModel>,
    ILoadEntity<AntrianMapHdrModel, IAntrianMapHdrKey>
{
}
