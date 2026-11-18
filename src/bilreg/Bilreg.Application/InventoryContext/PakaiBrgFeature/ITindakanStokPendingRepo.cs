using Bilreg.Domain.InventoryContext.PakaiBrgFeature;

namespace Bilreg.Application.InventoryContext.PakaiBrgFeature;

public interface ITindakanStokPendingRepo
{
    void Insert(TindakanStokPendingType model, string userId = "");
    void Insert(IEnumerable<TindakanStokPendingType> models, string userId = "");
    TindakanStokPendingType? GetData(string pendingId);
    IEnumerable<TindakanStokPendingType> ListByTindakanId(string tindakanId);
    IEnumerable<TindakanStokPendingType> ListFifoAktif(string brgId, string layananId);
    void MarkSettled(string pendingId, string pakaiBrgId, decimal qtySettled, DateTime settledDate, string updUser = "");
    void MarkSettled(string pendingId, string pakaiBrgId, DateTime settledDate, string updUser = "");
    void MarkVoid(string pendingId, DateTime voidDate, string updUser = "");
    void MarkVoidByTindakanId(string tindakanId, DateTime voidDate, string updUser = "");
}
