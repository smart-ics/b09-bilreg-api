using Bilreg.Application.ChargeContext.TarifFeature;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public class TarifOperationalStateRepo : ITarifOperationalStateRepo
{
    private const int SingleRowId = 1;
    private readonly ITarifOperationalStateDal _dal;

    public TarifOperationalStateRepo(ITarifOperationalStateDal dal) => _dal = dal;

    public TarifOperationalState GetState()
    {
        var dto = _dal.GetState();
        TarifMigrationMode? mode = dto.MigrationMode.HasValue
            ? (TarifMigrationMode)dto.MigrationMode.Value
            : null;

        return new TarifOperationalState(
            mode,
            dto.LastImportAt,
            dto.LastImportBy ?? "",
            dto.LastBaselineAt,
            dto.LastBaselinePolicyId ?? "",
            dto.UpdatedAt,
            dto.UpdatedBy ?? "");
    }

    public void SetMigrationModeOverride(TarifMigrationMode? mode, string userId)
    {
        var current = _dal.GetState();
        var updated = current with
        {
            MigrationMode = mode.HasValue ? (int)mode.Value : null,
            UpdatedAt = DateTime.Now,
            UpdatedBy = userId
        };
        _dal.UpsertState(updated);
    }

    public void RecordImport(string userId, DateTime importedAt)
    {
        var current = _dal.GetState();
        var updated = current with
        {
            LastImportAt = importedAt,
            LastImportBy = userId,
            UpdatedAt = importedAt,
            UpdatedBy = userId
        };
        _dal.UpsertState(updated);
    }

    public void RecordBaseline(string policyId, string userId, DateTime baselineAt)
    {
        var current = _dal.GetState();
        var updated = current with
        {
            LastBaselineAt = baselineAt,
            LastBaselinePolicyId = policyId,
            UpdatedAt = baselineAt,
            UpdatedBy = userId
        };
        _dal.UpsertState(updated);
    }
}
