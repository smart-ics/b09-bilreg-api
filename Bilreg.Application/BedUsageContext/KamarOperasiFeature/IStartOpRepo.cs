using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature;

public interface IStartOpRepo :
    ISaveChange<StartOpModel>,
    ILoadEntity<StartOpModel, IStartOpKey>,
    IDeleteEntity<IStartOpKey>
{
}
