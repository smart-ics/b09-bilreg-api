using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.InventoryContext.MutasiFeature;

public interface IMutasiBrgRepo :
    ISaveChange<MutasiBrgModel>,
    ILoadEntity<MutasiBrgModel, IMutasiBrgKey>,
    IDeleteEntity<IMutasiBrgKey>,
    IListData<MutasiBrgHeaderView, Periode>
{
    IEnumerable<MutasiBrgHeaderView> ListInTransit(string layananTujuanId);
}

public record MutasiBrgHeaderView(
    string MutasiBrgId,
    DateTime MutasiBrgDate,
    MutasiBrgStateEnum State,
    LayananReff LayananAsal,
    LayananReff LayananTujuan,
    string OrderMutasiId,
    string DispatchNote,
    string IssuerUserId) : IMutasiBrgKey;
