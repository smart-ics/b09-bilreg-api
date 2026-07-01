using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature;

public interface IDischargeOpRepo :
    ISaveChange<DischargeOpModel>,
    ILoadEntity<DischargeOpModel, IDischargeOpKey>,
    IDeleteEntity<IDischargeOpKey>,
    IListData<DischargeOpModel, DateTime>
{
}
