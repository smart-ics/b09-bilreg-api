using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public interface IOpCaseRepo :
    ISaveChange<OpCaseModel>,
    ILoadEntity<OpCaseModel, IOrderOpKey>,
    IDeleteEntity<IOrderOpKey>,
    IListData<OpCaseModel, Periode>
{
    IEnumerable<OpCaseReff> ListActiveOpCase();
}