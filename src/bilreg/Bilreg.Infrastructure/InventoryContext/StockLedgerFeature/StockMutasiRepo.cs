using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public class StockMutasiRepo : IStockMutasiRepo
{
    private readonly IStokMutasiDal _dal;

    public StockMutasiRepo(IStokMutasiDal dal) => _dal = dal;

    public void Insert(StockMovementModel model) => Insert(model, userId: "STL");

    public void Insert(StockMovementModel model, string userId)
    {
        var at = DateTime.Now;
        var auditUser = string.IsNullOrWhiteSpace(userId) ? "STL" : userId;
        _dal.Insert(StokMutasiDto.FromModel(model, crtUser: auditUser, crtDate: at, updUser: auditUser, updDate: at));
    }

    public bool Exists(string trsReffId, MovementKindEnum kind, string stokLokasiId) =>
        _dal.Exists(trsReffId, (int)kind, stokLokasiId);

    public bool ExistsReversalFor(string originalStokMutasiId) =>
        _dal.ExistsReversalFor(originalStokMutasiId);

    public IEnumerable<StockMovementModel> ListByTrsReffId(string trsReffId)
    {
        var list = _dal.ListByTrsReffId(trsReffId) ?? [];
        return list.Select(x => x.ToModel());
    }
}
