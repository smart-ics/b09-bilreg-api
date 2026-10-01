using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature;

//  M03-F01 P2-S04 — follow-up order persistence contract (TD-08).
//  Outstanding scopes: single visit (RegId) and across visits
//  (supervision). Cancellation-only-before-reception and idempotent
//  confirm are enforced by the aggregate, not here.
public interface ITindakanLanjutRepo :
    ISaveChange<TindakanLanjutModel>,
    ILoadEntity<TindakanLanjutModel, ITindakanLanjutKey>,
    IListData<TindakanLanjutModel, IRegKey>
{
    IEnumerable<TindakanLanjutModel> ListOutstanding(IRegKey regKey);
    IEnumerable<TindakanLanjutModel> ListOutstandingAll();
}
