using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S2 — Outcome of pure sync delta interpretation (no I/O / TX).
/// </summary>
public enum LegacySyncInterpretationOutcomeEnum
{
    Succeeded = 1,
    RequiresScopedReDerive = 2,
    Ambiguous = 3
}

/// <summary>
/// Explicit accountable intent kinds produced from discovery deltas.
/// </summary>
public enum LegacySyncIntentKindEnum
{
    ReversePriorMovement = 1,
    CorrectPriorMovement = 2,
    ApplyLegacySynchronizedReceipt = 3,
    ApplyLegacySynchronizedOutbound = 4,
    AdjustLayerRemainingQuantity = 5,
    RepresentationalBalanceOmission = 6
}

/// <summary>
/// One proposed Stock Ledger sync intent. Persistence is owned by P3-S4.
/// </summary>
public sealed record LegacySyncIntentType(
    LegacySyncIntentKindEnum Kind,
    string? LegacyJournalId,
    string? LayananId,
    string? LegacyRowId,
    string? TargetMovementId,
    string? TargetLayerId,
    StockMovementModel? ProposedMovement,
    string SyncIdempotencyKey,
    string? Explanation);

/// <summary>
/// Deterministic result of <see cref="LegacySyncDeltaInterpreter.Interpret"/>.
/// Fail-closed outcomes never carry a partial intent list.
/// </summary>
public sealed record LegacySyncInterpretationResult
{
    private LegacySyncInterpretationResult(
        LegacySyncInterpretationOutcomeEnum outcome,
        IReadOnlyList<LegacySyncIntentType> intents,
        string? explanation)
    {
        Outcome = outcome;
        Intents = intents;
        Explanation = explanation;
    }

    public LegacySyncInterpretationOutcomeEnum Outcome { get; }
    public IReadOnlyList<LegacySyncIntentType> Intents { get; }
    public string? Explanation { get; }

    public bool IsSucceeded => Outcome == LegacySyncInterpretationOutcomeEnum.Succeeded;
    public bool IsFailClosed => Outcome is LegacySyncInterpretationOutcomeEnum.RequiresScopedReDerive
        or LegacySyncInterpretationOutcomeEnum.Ambiguous;

    public static LegacySyncInterpretationResult Succeeded(IReadOnlyList<LegacySyncIntentType> intents)
        => new(LegacySyncInterpretationOutcomeEnum.Succeeded, intents, explanation: null);

    public static LegacySyncInterpretationResult RequiresScopedReDerive(string explanation)
        => new(
            LegacySyncInterpretationOutcomeEnum.RequiresScopedReDerive,
            Array.Empty<LegacySyncIntentType>(),
            explanation);

    public static LegacySyncInterpretationResult Ambiguous(string explanation)
        => new(
            LegacySyncInterpretationOutcomeEnum.Ambiguous,
            Array.Empty<LegacySyncIntentType>(),
            explanation);
}
