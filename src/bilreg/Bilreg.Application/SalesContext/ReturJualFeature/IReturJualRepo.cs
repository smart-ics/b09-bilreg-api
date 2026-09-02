using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.ReturJualFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.SalesContext.ReturJualFeature;

public interface IReturJualRepo :
    ISaveChange<ReturJualModel>,
    ILoadEntity<ReturJualModel, IReturJualKey>,
    IDeleteEntity<IReturJualKey>,
    IListData<ReturJualModel, IRegKey>
{
    IEnumerable<ReturJualItemQtyDto> ListReturQtyByPenjualan(
        IPenjualanKey jualKey, IReturJualKey returKey);
}

public record ReturJualItemQtyDto(string BrgId, decimal QtyRetur, string SatuanId);