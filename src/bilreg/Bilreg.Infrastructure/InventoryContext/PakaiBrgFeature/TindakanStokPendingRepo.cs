using Bilreg.Application.InventoryContext.PakaiBrgFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;

namespace Bilreg.Infrastructure.InventoryContext.PakaiBrgFeature;

public class TindakanStokPendingRepo : ITindakanStokPendingRepo
{
    private readonly ITindakanStokPendingDal _dal;

    public TindakanStokPendingRepo(ITindakanStokPendingDal dal)
    {
        _dal = dal;
    }

    public void Insert(TindakanStokPendingType model, string userId = "")
    {
        var dto = TindakanStokPendingDto.FromModel(model, userId);
        _dal.Insert(dto);
    }

    public void Insert(IEnumerable<TindakanStokPendingType> models, string userId = "")
    {
        var listDto = models.Select(m => TindakanStokPendingDto.FromModel(m, userId));
        _dal.Insert(listDto);
    }

    public TindakanStokPendingType? GetData(string pendingId)
    {
        var dto = _dal.GetData(pendingId);
        return dto?.ToModel();
    }

    public IEnumerable<TindakanStokPendingType> ListByTindakanId(string tindakanId)
    {
        var listDto = _dal.ListByTindakanId(tindakanId)?.ToList() ?? [];
        return listDto.Select(x => x.ToModel());
    }

    public IEnumerable<TindakanStokPendingType> ListFifoAktif(string brgId, string layananId)
    {
        var listDto = _dal.ListFifoAktif(brgId, layananId)?.ToList() ?? [];
        return listDto.Select(x => x.ToModel());
    }

    public void MarkSettled(string pendingId, string pakaiBrgId, decimal qtySettled, DateTime settledDate, string updUser = "")
    {
        _dal.MarkSettled(pendingId, pakaiBrgId, qtySettled, settledDate, updUser);
    }

    public void MarkSettled(string pendingId, string pakaiBrgId, DateTime settledDate, string updUser = "")
    {
        _dal.MarkSettled(pendingId, pakaiBrgId, 0m, settledDate, updUser);
    }

    public void MarkVoid(string pendingId, DateTime voidDate, string updUser = "")
    {
        _dal.MarkVoid(pendingId, voidDate, updUser);
    }

    public void MarkVoidByTindakanId(string tindakanId, DateTime voidDate, string updUser = "")
    {
        _dal.MarkVoidByTindakanId(tindakanId, voidDate, updUser);
    }
}
