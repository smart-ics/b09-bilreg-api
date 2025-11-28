using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public interface IOpCaseRepo :
    ISaveChange<OpCaseModel>,
    ILoadEntity<OpCaseModel, IOrderOpKey>,
    IDeleteEntity<IOrderOpKey>
{
    IEnumerable<OpCaseOrderView> ListActiveOpCase();
    
}

public record OpCaseOrderView(string OrderOpId,
    PasienReff Pasien, string NamaOperasi, 
    UrgencyLevelEnum UrgencyLevel, DateTime PreferedDate);