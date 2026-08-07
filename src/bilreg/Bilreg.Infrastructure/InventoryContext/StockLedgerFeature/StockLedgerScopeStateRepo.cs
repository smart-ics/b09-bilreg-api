using System.Data.SqlClient;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public class StockLedgerScopeStateRepo : IStockLedgerScopeStateRepo
{
    private readonly IStockLedgerScopeDal _dal;

    public StockLedgerScopeStateRepo(IStockLedgerScopeDal dal) => _dal = dal;

    public void SaveChanges(StockLedgerScopeStateModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(StockLedgerScopeDto.FromModel(model)),
                onNone: () => _dal.Insert(StockLedgerScopeDto.FromModel(model)));
    }

    public MayBe<StockLedgerScopeStateModel> LoadEntity(IStockLedgerScopeKey key)
    {
        var dto = _dal.GetData(key);
        if (dto is null)
            return MayBe<StockLedgerScopeStateModel>.None;
        return MayBe.From(dto.ToModel());
    }

    public bool TryInsertNew(StockLedgerScopeStateModel model)
    {
        try
        {
            _dal.Insert(StockLedgerScopeDto.FromModel(model));
            return true;
        }
        catch (SqlException ex) when (IsUniqueViolation(ex))
        {
            return false;
        }
    }

    public bool TryUpdateWhenReconstructionStatus(
        StockLedgerScopeStateModel model,
        ReconstructionStatusEnum expectedPriorStatus)
        => _dal.UpdateWhenReconstructionStatus(
               StockLedgerScopeDto.FromModel(model),
               (int)expectedPriorStatus)
           == 1;

    private static bool IsUniqueViolation(SqlException ex)
        => ex.Number is 2601 or 2627;
}
