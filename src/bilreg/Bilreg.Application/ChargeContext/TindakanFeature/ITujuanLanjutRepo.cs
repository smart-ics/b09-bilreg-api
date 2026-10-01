using Bilreg.Domain.ChargeContext.TindakanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature;

//  M03-F01 P2-S04 — order-type to receiving-service mapping contract (TD-04).
//  Read-only: mapping rows are seeded by P1-S02, resolved through
//  TujuanLanjutService (no per-order-type branching).
public interface ITujuanLanjutRepo :
    ILoadEntity<TujuanLanjutType, ITujuanLanjutKey>,
    IListData<TujuanLanjutType>
{
}
