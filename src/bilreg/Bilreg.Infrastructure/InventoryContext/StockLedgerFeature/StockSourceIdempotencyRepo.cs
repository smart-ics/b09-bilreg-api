using System.Data.SqlClient;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public class StockSourceIdempotencyRepo : IStockSourceIdempotencyRepo
{
    private readonly IStockSourceIdempotencyDal _dal;

    public StockSourceIdempotencyRepo(IStockSourceIdempotencyDal dal) => _dal = dal;

    public MayBe<StockSourceIdempotencyModel> LoadEntity(IStockSourceIdempotencyKey key)
    {
        var dto = _dal.GetData(key);
        if (dto is null)
            return MayBe<StockSourceIdempotencyModel>.None;
        return MayBe.From(dto.ToModel());
    }

    public MayBe<StockSourceIdempotencyModel> LoadByBusinessKey(IStockSourceIdempotencyBusinessKey key)
    {
        var dto = _dal.GetDataByBusinessKey(key);
        if (dto is null)
            return MayBe<StockSourceIdempotencyModel>.None;
        return MayBe.From(dto.ToModel());
    }

    public StockSourceIdempotencyInsertResult InsertOrGetExisting(StockSourceIdempotencyModel model)
    {
        var existing = _dal.GetDataByBusinessKey(model);
        if (existing is not null)
            return new StockSourceIdempotencyInsertResult(WasInserted: false, existing.ToModel());

        try
        {
            _dal.Insert(StockSourceIdempotencyDto.FromModel(model));
            return new StockSourceIdempotencyInsertResult(WasInserted: true, model);
        }
        catch (SqlException ex) when (IsUniqueViolation(ex))
        {
            // Concurrent insert of the same (Kind, Key) — return the winner's durable row.
            var raced = _dal.GetDataByBusinessKey(model)
                ?? throw StockLedgerPersistenceException.Integrity(
                    $"Idempotency key '{model.IdempotencyKind}/{model.IdempotencyKey}' " +
                    "conflicted but the existing row could not be loaded.");

            return new StockSourceIdempotencyInsertResult(WasInserted: false, raced.ToModel());
        }
    }

    private static bool IsUniqueViolation(SqlException ex)
        => ex.Number is 2601 or 2627;
}
