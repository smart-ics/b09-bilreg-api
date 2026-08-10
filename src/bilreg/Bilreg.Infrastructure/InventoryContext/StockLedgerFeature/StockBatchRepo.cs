using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public class StockBatchRepo : IStockBatchRepo
{
    private readonly IStockBatchDal _batchDal;
    private readonly IStokLokasiDal _lokasiDal;

    public StockBatchRepo(IStockBatchDal batchDal, IStokLokasiDal lokasiDal)
    {
        _batchDal = batchDal;
        _lokasiDal = lokasiDal;
    }

    public MayBe<StockBatchModel> LoadEntity(IStockBatchKey key)
    {
        var batchDto = _batchDal.GetData(key);
        if (batchDto is null)
            return MayBe<StockBatchModel>.None;

        var listLokasi = (_lokasiDal.ListByStokBatchId(batchDto.StokBatchId) ?? [])
            .Select(x => x.ToModel())
            .ToList();
        return MayBe.From(batchDto.ToModel(listLokasi));
    }

    public MayBe<StockBatchModel> LoadByNaturalKey(string brgId, string brgMasukReffId)
    {
        var batchDto = _batchDal.GetByNaturalKey(brgId, brgMasukReffId);
        if (batchDto is null)
            return MayBe<StockBatchModel>.None;

        var listLokasi = (_lokasiDal.ListByStokBatchId(batchDto.StokBatchId) ?? [])
            .Select(x => x.ToModel())
            .ToList();
        return MayBe.From(batchDto.ToModel(listLokasi));
    }

    public IEnumerable<LocationStockBalanceModel> ListAllocationCandidates(string brgId, string layananId)
    {
        var list = _lokasiDal.ListAllocationCandidates(brgId, layananId) ?? [];
        return list.Select(x => x.ToModel());
    }

    public void SaveChanges(StockBatchModel model) => SaveChanges(model, userId: "STL");

    public void SaveChanges(StockBatchModel model, string userId)
    {
        var at = DateTime.Now;
        var auditUser = string.IsNullOrWhiteSpace(userId) ? "STL" : userId;
        var stored = _batchDal.GetData(model);
        var batchDto = StockBatchDto.FromModel(model, crtUser: auditUser, crtDate: at, updUser: auditUser, updDate: at);

        if (stored is null)
        {
            _batchDal.Insert(batchDto);
            model.AcceptPersisted();
        }
        else if (model.Version != model.PersistedVersion)
        {
            if (_batchDal.UpdateConditional(batchDto, model.PersistedVersion) != 1)
                throw new InvalidOperationException(
                    $"StockBatch '{model.StokBatchId}' concurrency conflict " +
                    $"(expected Version={model.PersistedVersion}).");
            model.AcceptPersisted();
        }

        foreach (var lokasi in model.ListLokasi)
        {
            var storedLokasi = _lokasiDal.GetData(lokasi);
            var lokasiDto = StokLokasiDto.FromModel(lokasi, crtUser: auditUser, crtDate: at, updUser: auditUser, updDate: at);

            if (storedLokasi is null)
            {
                _lokasiDal.Insert(lokasiDto);
                lokasi.AcceptPersisted();
            }
            else if (lokasi.Version != lokasi.PersistedVersion)
            {
                if (_lokasiDal.UpdateConditional(lokasiDto, lokasi.PersistedVersion) != 1)
                    throw new InvalidOperationException(
                        $"StokLokasi '{lokasi.StokLokasiId}' concurrency conflict " +
                        $"(expected Version={lokasi.PersistedVersion}).");
                lokasi.AcceptPersisted();
            }
        }
        // Never DELETE lokasi — depleted balances retained (BR-STL-011, BR-STL-012).
    }
}
