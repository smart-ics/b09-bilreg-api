using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature;

public interface IOrderOpRepo :
    ISaveChange<OrderOpModel>,
    ILoadEntity<OrderOpModel, IOrderOpKey>,
    IDeleteEntity<IOrderOpKey>
{
}