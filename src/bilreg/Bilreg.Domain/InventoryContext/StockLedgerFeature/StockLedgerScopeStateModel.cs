using Ardalis.GuardClauses;
using Bilreg.Domain.BrgContext.BrgFeature;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Coexistence state for one Reconstruction Scope (Item + Receipt Source across all locations).
/// Holds Reconstruction Status, Synchronization State/Position, and inconsistency reason shapes only.
/// Does not encode runtime authority or compute legacy fingerprints.
/// </summary>
public record StockLedgerScopeStateModel : IStockLedgerScopeKey
{
    #region CREATION
    private StockLedgerScopeStateModel(
        string brgId,
        string receiptSourceId,
        ReconstructionStatusEnum reconstructionStatus,
        SynchronizationStateEnum synchronizationState,
        SynchronizationPositionType? synchronizationPosition,
        string? reconstructionBasisVersion,
        string? inconsistencyReason)
    {
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        Guard.Against.NullOrWhiteSpace(receiptSourceId, nameof(receiptSourceId));
        Guard.Against.EnumOutOfRange(reconstructionStatus, nameof(reconstructionStatus));
        Guard.Against.EnumOutOfRange(synchronizationState, nameof(synchronizationState));

        BrgId = brgId;
        ReceiptSourceId = receiptSourceId;
        ReconstructionStatus = reconstructionStatus;
        SynchronizationState = synchronizationState;
        SynchronizationPosition = synchronizationPosition;
        ReconstructionBasisVersion = reconstructionBasisVersion;
        InconsistencyReason = inconsistencyReason;
    }

    /// <summary>
    /// Initial coexistence state: not yet reconstructed; no Synchronization Position.
    /// Synchronization State starts as <see cref="SynchronizationStateEnum.Current"/> because
    /// there is not yet a Stock Ledger baseline that can be stale relative to legacy authority.
    /// </summary>
    public static StockLedgerScopeStateModel CreateNotReconstructed(IStockLedgerScopeKey scope)
    {
        Guard.Against.Null(scope, nameof(scope));
        return new StockLedgerScopeStateModel(
            scope.BrgId,
            scope.ReceiptSourceId,
            ReconstructionStatusEnum.NotReconstructed,
            SynchronizationStateEnum.Current,
            synchronizationPosition: null,
            reconstructionBasisVersion: null,
            inconsistencyReason: null);
    }

    public static StockLedgerScopeStateModel CreateNotReconstructed(IBrgKey item, IReceiptSourceKey receiptSource)
    {
        Guard.Against.Null(item, nameof(item));
        Guard.Against.Null(receiptSource, nameof(receiptSource));
        return CreateNotReconstructed(StockLedgerScopeKeyType.Create(item, receiptSource));
    }
    #endregion

    #region PROPERTIES
    public string BrgId { get; init; }
    public string ReceiptSourceId { get; init; }
    public ReconstructionStatusEnum ReconstructionStatus { get; init; }
    public SynchronizationStateEnum SynchronizationState { get; init; }
    public SynchronizationPositionType? SynchronizationPosition { get; init; }
    /// <summary>Optional reconstruction basis / version label (G-04 shape; opaque to this model).</summary>
    public string? ReconstructionBasisVersion { get; init; }
    public string? InconsistencyReason { get; init; }

    public StockLedgerScopeKeyType ScopeKey
        => StockLedgerScopeKeyType.Create(BrgId, ReceiptSourceId);

    public bool HasSynchronizationPosition => SynchronizationPosition is not null;
    #endregion

    #region RECONSTRUCTION TRANSITIONS
    /// <summary>
    /// NotReconstructed → ReconstructionRequired.
    /// Safe without live discovery: records that a baseline is needed.
    /// </summary>
    public StockLedgerScopeStateModel RequireReconstruction()
    {
        EnsureReconstructionStatus(
            ReconstructionStatusEnum.NotReconstructed,
            nameof(RequireReconstruction));

        return WithState(
            reconstructionStatus: ReconstructionStatusEnum.ReconstructionRequired,
            inconsistencyReason: null);
    }

    /// <summary>
    /// ReconstructionRequired → Reconstructing (Phase A claim shape).
    /// Does not read legacy SQL.
    /// </summary>
    public StockLedgerScopeStateModel BeginReconstruction()
    {
        EnsureReconstructionStatus(
            ReconstructionStatusEnum.ReconstructionRequired,
            nameof(BeginReconstruction));

        return WithState(
            reconstructionStatus: ReconstructionStatusEnum.Reconstructing,
            inconsistencyReason: null);
    }

    /// <summary>
    /// Reconstructing → Reconstructed. Initializes opaque Synchronization Position + algorithm version.
    /// Caller supplies the position after external calculation; this method does not compute fingerprints.
    /// </summary>
    public StockLedgerScopeStateModel CompleteReconstruction(
        SynchronizationPositionType synchronizationPosition,
        string? reconstructionBasisVersion = null)
    {
        EnsureReconstructionStatus(
            ReconstructionStatusEnum.Reconstructing,
            nameof(CompleteReconstruction));
        Guard.Against.Null(synchronizationPosition, nameof(synchronizationPosition));

        return WithState(
            reconstructionStatus: ReconstructionStatusEnum.Reconstructed,
            synchronizationState: SynchronizationStateEnum.Current,
            synchronizationPosition: synchronizationPosition,
            reconstructionBasisVersion: NormalizeOptional(reconstructionBasisVersion),
            setReconstructionBasisVersion: true,
            inconsistencyReason: null);
    }

    /// <summary>
    /// Reconstructing → Inconsistent when available legacy facts cannot form a balanced baseline.
    /// </summary>
    public StockLedgerScopeStateModel MarkReconstructionInconsistent(string reason)
    {
        EnsureReconstructionStatus(
            ReconstructionStatusEnum.Reconstructing,
            nameof(MarkReconstructionInconsistent));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));

        return WithState(
            reconstructionStatus: ReconstructionStatusEnum.Inconsistent,
            synchronizationState: SynchronizationStateEnum.Inconsistent,
            clearSynchronizationPosition: true,
            reconstructionBasisVersion: null,
            setReconstructionBasisVersion: true,
            inconsistencyReason: reason.Trim());
    }
    #endregion

    #region SYNCHRONIZATION TRANSITIONS
    /// <summary>
    /// Current → LegacyChangePending. Requires a reconstructed baseline.
    /// </summary>
    public StockLedgerScopeStateModel MarkLegacyChangePending()
    {
        EnsureReconstructedBaseline(nameof(MarkLegacyChangePending));
        EnsureSynchronizationState(
            SynchronizationStateEnum.Current,
            nameof(MarkLegacyChangePending));

        return WithState(
            synchronizationState: SynchronizationStateEnum.LegacyChangePending,
            inconsistencyReason: null);
    }

    /// <summary>
    /// Current or LegacyChangePending → SynchronizationRequired.
    /// Explicit mark only — does not discover or apply legacy changes.
    /// </summary>
    public StockLedgerScopeStateModel RequireSynchronization()
    {
        EnsureReconstructedBaseline(nameof(RequireSynchronization));

        if (SynchronizationState is not (
                SynchronizationStateEnum.Current
                or SynchronizationStateEnum.LegacyChangePending))
        {
            throw new InvalidOperationException(
                $"Cannot {nameof(RequireSynchronization)} from Synchronization State '{SynchronizationState}'.");
        }

        return WithState(
            synchronizationState: SynchronizationStateEnum.SynchronizationRequired,
            inconsistencyReason: null);
    }

    /// <summary>
    /// SynchronizationRequired → Current. Advances opaque Synchronization Position after successful catch-up.
    /// Does not compute the next fingerprint; caller supplies the new opaque value.
    /// </summary>
    public StockLedgerScopeStateModel CompleteSynchronization(SynchronizationPositionType synchronizationPosition)
    {
        EnsureReconstructedBaseline(nameof(CompleteSynchronization));
        EnsureSynchronizationState(
            SynchronizationStateEnum.SynchronizationRequired,
            nameof(CompleteSynchronization));
        Guard.Against.Null(synchronizationPosition, nameof(synchronizationPosition));

        return WithState(
            synchronizationState: SynchronizationStateEnum.Current,
            synchronizationPosition: synchronizationPosition,
            inconsistencyReason: null);
    }

    /// <summary>
    /// LegacyChangePending or SynchronizationRequired → Inconsistent.
    /// Reconstruction Status remains Reconstructed; origin of facts is unaffected.
    /// </summary>
    public StockLedgerScopeStateModel MarkSynchronizationInconsistent(string reason)
    {
        EnsureReconstructedBaseline(nameof(MarkSynchronizationInconsistent));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));

        if (SynchronizationState is not (
                SynchronizationStateEnum.LegacyChangePending
                or SynchronizationStateEnum.SynchronizationRequired))
        {
            throw new InvalidOperationException(
                $"Cannot {nameof(MarkSynchronizationInconsistent)} from Synchronization State '{SynchronizationState}'.");
        }

        return WithState(
            synchronizationState: SynchronizationStateEnum.Inconsistent,
            inconsistencyReason: reason.Trim());
    }
    #endregion

    #region INVARIANTS
    private StockLedgerScopeStateModel WithState(
        ReconstructionStatusEnum? reconstructionStatus = null,
        SynchronizationStateEnum? synchronizationState = null,
        SynchronizationPositionType? synchronizationPosition = null,
        bool clearSynchronizationPosition = false,
        string? reconstructionBasisVersion = null,
        bool setReconstructionBasisVersion = false,
        string? inconsistencyReason = null)
    {
        return new StockLedgerScopeStateModel(
            BrgId,
            ReceiptSourceId,
            reconstructionStatus ?? ReconstructionStatus,
            synchronizationState ?? SynchronizationState,
            clearSynchronizationPosition
                ? null
                : (synchronizationPosition ?? SynchronizationPosition),
            setReconstructionBasisVersion || reconstructionBasisVersion is not null
                ? reconstructionBasisVersion
                : ReconstructionBasisVersion,
            inconsistencyReason);
    }

    private void EnsureReconstructionStatus(
        ReconstructionStatusEnum expected,
        string operation)
    {
        if (ReconstructionStatus != expected)
        {
            throw new InvalidOperationException(
                $"Cannot {operation} from Reconstruction Status '{ReconstructionStatus}' (expected '{expected}').");
        }
    }

    private void EnsureSynchronizationState(
        SynchronizationStateEnum expected,
        string operation)
    {
        if (SynchronizationState != expected)
        {
            throw new InvalidOperationException(
                $"Cannot {operation} from Synchronization State '{SynchronizationState}' (expected '{expected}').");
        }
    }

    private void EnsureReconstructedBaseline(string operation)
    {
        if (ReconstructionStatus != ReconstructionStatusEnum.Reconstructed)
        {
            throw new InvalidOperationException(
                $"Cannot {operation} unless Reconstruction Status is '{ReconstructionStatusEnum.Reconstructed}'.");
        }

        if (SynchronizationPosition is null)
        {
            throw new InvalidOperationException(
                $"Cannot {operation} without an initialized Synchronization Position.");
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    #endregion
}
