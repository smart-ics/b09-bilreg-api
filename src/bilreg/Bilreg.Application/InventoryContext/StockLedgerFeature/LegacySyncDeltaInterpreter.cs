using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S2 — Pure, side-effect-free, SQL-free interpreter that maps Legacy Change Discovery
/// deltas (+ caller-supplied in-memory Ledger snapshot) to accountable sync intents.
/// Persistence, Scope transitions, and Synchronization Position advancement belong to P3-S4.
/// </summary>
public static class LegacySyncDeltaInterpreter
{
    /// <summary>
    /// Deterministic Effective Business Time when balance-only deltas supply no mutation time.
    /// </summary>
    public static readonly DateTime UnknownSyncEffectiveTime =
        new(1900, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    /// <summary>
    /// Deterministic interpretation. Equivalent inputs produce equivalent intents.
    /// Fail-closed outcomes never carry a partial intent list.
    /// </summary>
    public static LegacySyncInterpretationResult Interpret(
        IStockLedgerScopeKey scope,
        LegacyChangeDiscoveryResult discovery,
        LegacySyncLedgerSnapshot ledgerSnapshot,
        IReadOnlyList<LegacyStockJournalEntryType> currentJournals,
        IReadOnlyList<LegacyStockBalanceType> currentBalances)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(discovery);
        ArgumentNullException.ThrowIfNull(ledgerSnapshot);
        ArgumentNullException.ThrowIfNull(currentJournals);
        ArgumentNullException.ThrowIfNull(currentBalances);

        if (discovery.Outcome == LegacyChangeDiscoveryOutcomeEnum.Undeterminable)
        {
            return LegacySyncInterpretationResult.Ambiguous(
                discovery.Explanation
                ?? "Discovery outcome is Undeterminable; sync intents cannot be proposed safely.");
        }

        if (discovery.Deltas.Any(d => d.Kind == LegacyDiscoveredDeltaKindEnum.RequiresScopedReDerive))
        {
            var reason = discovery.Deltas
                .First(d => d.Kind == LegacyDiscoveredDeltaKindEnum.RequiresScopedReDerive)
                .Explanation
                ?? discovery.Explanation
                ?? "Discovery requires scoped re-derive; no partial sync intents are proposed.";
            return LegacySyncInterpretationResult.RequiresScopedReDerive(reason);
        }

        if (discovery.Outcome == LegacyChangeDiscoveryOutcomeEnum.Unchanged
            || discovery.Deltas.Count == 0)
        {
            return LegacySyncInterpretationResult.Succeeded(Array.Empty<LegacySyncIntentType>());
        }

        var journalsByIdentity = currentJournals
            .GroupBy(j => new LegacyJournalIdentity(j.LayananId.Trim(), j.LegacyJournalId.Trim()))
            .ToDictionary(g => g.Key, g => g.First());

        var balancesByIdentity = currentBalances
            .Where(b => !string.IsNullOrWhiteSpace(b.LegacyRowId))
            .GroupBy(b => new LegacyBalanceIdentity(b.LayananId.Trim(), b.LegacyRowId!.Trim()))
            .ToDictionary(g => g.Key, g => g.First());

        var ordered = Deduplicate(OrderDeltas(discovery.Deltas));
        var intents = new List<LegacySyncIntentType>(ordered.Count);

        foreach (var delta in ordered)
        {
            var step = InterpretOne(
                scope,
                delta,
                ledgerSnapshot,
                journalsByIdentity,
                balancesByIdentity);

            if (step.FailClosed is { } failClosed)
                return failClosed;

            if (step.Intent is { } intent)
                intents.Add(intent);
        }

        return LegacySyncInterpretationResult.Succeeded(intents);
    }

    private static InterpretStep InterpretOne(
        IStockLedgerScopeKey scope,
        LegacyDiscoveredDeltaType delta,
        LegacySyncLedgerSnapshot snapshot,
        IReadOnlyDictionary<LegacyJournalIdentity, LegacyStockJournalEntryType> journalsByIdentity,
        IReadOnlyDictionary<LegacyBalanceIdentity, LegacyStockBalanceType> balancesByIdentity)
        => delta.Kind switch
        {
            LegacyDiscoveredDeltaKindEnum.JournalVoidDelete
                => InterpretJournalVoidDelete(scope, delta, snapshot),
            LegacyDiscoveredDeltaKindEnum.JournalUpdate
                => InterpretJournalUpdate(scope, delta, snapshot, journalsByIdentity),
            LegacyDiscoveredDeltaKindEnum.JournalInsert
                => InterpretJournalInsert(scope, delta, journalsByIdentity),
            LegacyDiscoveredDeltaKindEnum.BalanceUpdate
                => InterpretBalanceUpdate(scope, delta, snapshot, balancesByIdentity),
            LegacyDiscoveredDeltaKindEnum.BalanceDelete
                => InterpretBalanceDelete(scope, delta, snapshot),
            LegacyDiscoveredDeltaKindEnum.RequiresScopedReDerive
                => InterpretStep.Fail(
                    LegacySyncInterpretationResult.RequiresScopedReDerive(
                        delta.Explanation ?? "RequiresScopedReDerive delta encountered.")),
            _ => InterpretStep.Fail(
                LegacySyncInterpretationResult.Ambiguous(
                    $"Unsupported discovery delta kind '{delta.Kind}'."))
        };

    private static InterpretStep InterpretJournalVoidDelete(
        IStockLedgerScopeKey scope,
        LegacyDiscoveredDeltaType delta,
        LegacySyncLedgerSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(delta.LegacyJournalId) || string.IsNullOrWhiteSpace(delta.LayananId))
        {
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(
                "JournalVoidDelete delta is missing LegacyJournalId or LayananId."));
        }

        var identity = new LegacyJournalIdentity(delta.LayananId.Trim(), delta.LegacyJournalId.Trim());
        if (!snapshot.JournalAnchors.TryGetValue(identity, out var anchor)
            || string.IsNullOrWhiteSpace(anchor.StockMovementId))
        {
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(
                $"JournalVoidDelete for '{identity.LegacyJournalId}' at '{identity.LayananId}' " +
                "has no Ledger journal anchor; cannot propose accountable reversal without inventing history erase."));
        }

        var prior = ResolveMovement(snapshot, anchor);
        if (prior is null)
        {
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(
                $"JournalVoidDelete for '{identity.LegacyJournalId}' references movement " +
                $"'{anchor.StockMovementId}' but the movement was not supplied in the Ledger snapshot."));
        }

        var effectiveTime = delta.MutationTime ?? prior.EffectiveBusinessTime;
        var sourceRef = SourceTransactionReferenceType.Create(
            $"SYNC-VOID|{identity.LegacyJournalId}|{identity.LayananId}");
        var movementId = BuildMovementId(scope, "REV", identity.LayananId, identity.LegacyJournalId);
        var reversal = prior.Reverse(
            sourceRef,
            effectiveTime,
            StockFactOriginEnum.LegacySynchronized,
            movementId);

        var idempotencyKey = BuildVoidIdempotencyKey(scope, identity, prior);

        return InterpretStep.Ok(new LegacySyncIntentType(
            LegacySyncIntentKindEnum.ReversePriorMovement,
            identity.LegacyJournalId,
            identity.LayananId,
            LegacyRowId: null,
            TargetMovementId: prior.StockMovementId,
            TargetLayerId: null,
            ProposedMovement: reversal,
            SyncIdempotencyKey: idempotencyKey,
            Explanation: delta.Explanation
                ?? "Legacy journal void-delete → accountable reversal (history retained)."));
    }

    private static InterpretStep InterpretJournalUpdate(
        IStockLedgerScopeKey scope,
        LegacyDiscoveredDeltaType delta,
        LegacySyncLedgerSnapshot snapshot,
        IReadOnlyDictionary<LegacyJournalIdentity, LegacyStockJournalEntryType> journalsByIdentity)
    {
        if (string.IsNullOrWhiteSpace(delta.LegacyJournalId) || string.IsNullOrWhiteSpace(delta.LayananId))
        {
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(
                "JournalUpdate delta is missing LegacyJournalId or LayananId."));
        }

        var identity = new LegacyJournalIdentity(delta.LayananId.Trim(), delta.LegacyJournalId.Trim());
        if (!snapshot.JournalAnchors.TryGetValue(identity, out var anchor)
            || string.IsNullOrWhiteSpace(anchor.StockMovementId))
        {
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(
                $"JournalUpdate for '{identity.LegacyJournalId}' has no Ledger journal anchor."));
        }

        var prior = ResolveMovement(snapshot, anchor);
        if (prior is null)
        {
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(
                $"JournalUpdate for '{identity.LegacyJournalId}' references movement " +
                $"'{anchor.StockMovementId}' but the movement was not supplied in the Ledger snapshot."));
        }

        if (!journalsByIdentity.TryGetValue(identity, out var currentJournal))
        {
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(
                $"JournalUpdate for '{identity.LegacyJournalId}' has no matching current tb_buku row."));
        }

        if (!TryBuildCompensatoryCorrectionLine(
                scope,
                prior,
                currentJournal,
                out var compensationLine,
                out var skipQuantityConsequence,
                out var ambiguity))
        {
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(ambiguity!));
        }

        if (skipQuantityConsequence)
        {
            // Quantity-neutral journal update with preserved material attributes —
            // do not emit a Correct that restates absolute quantity.
            return InterpretStep.Skip();
        }

        var sourceRef = SourceTransactionReferenceType.Create(
            string.IsNullOrWhiteSpace(currentJournal.MutationTransactionId)
                ? $"SYNC-CORR|{currentJournal.LegacyJournalId}"
                : currentJournal.MutationTransactionId.Trim());
        var movementId = BuildMovementId(scope, "COR", identity.LayananId, identity.LegacyJournalId);
        var correction = prior.Correct(
            sourceRef,
            currentJournal.MutationTime,
            [compensationLine!],
            StockFactOriginEnum.LegacySynchronized,
            movementId);

        return InterpretStep.Ok(new LegacySyncIntentType(
            LegacySyncIntentKindEnum.CorrectPriorMovement,
            identity.LegacyJournalId,
            identity.LayananId,
            LegacyRowId: null,
            TargetMovementId: prior.StockMovementId,
            TargetLayerId: null,
            ProposedMovement: correction,
            SyncIdempotencyKey: LegacyChangeDiscoveryIdentityKeys.BuildJournalKey(scope, currentJournal),
            Explanation: delta.Explanation
                ?? "Legacy journal quantity update → compensatory correction delta (history retained)."));
    }

    private static InterpretStep InterpretJournalInsert(
        IStockLedgerScopeKey scope,
        LegacyDiscoveredDeltaType delta,
        IReadOnlyDictionary<LegacyJournalIdentity, LegacyStockJournalEntryType> journalsByIdentity)
    {
        if (string.IsNullOrWhiteSpace(delta.LegacyJournalId) || string.IsNullOrWhiteSpace(delta.LayananId))
        {
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(
                "JournalInsert delta is missing LegacyJournalId or LayananId."));
        }

        var identity = new LegacyJournalIdentity(delta.LayananId.Trim(), delta.LegacyJournalId.Trim());
        if (!journalsByIdentity.TryGetValue(identity, out var journal))
        {
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(
                $"JournalInsert for '{identity.LegacyJournalId}' has no matching current tb_buku row."));
        }

        if (!TryBuildJournalLine(scope, journal, out var line, out var lineAmbiguity))
        {
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(lineAmbiguity!));
        }

        var sourceRef = SourceTransactionReferenceType.Create(
            string.IsNullOrWhiteSpace(journal.MutationTransactionId)
                ? $"SYNC-INS|{journal.LegacyJournalId}"
                : journal.MutationTransactionId.Trim());
        var movementId = BuildMovementId(scope, "INS", identity.LayananId, identity.LegacyJournalId);

        StockMovementModel movement;
        LegacySyncIntentKindEnum kind;
        if (journal.QuantityIn > 0m && journal.QuantityOut == 0m)
        {
            kind = LegacySyncIntentKindEnum.ApplyLegacySynchronizedReceipt;
            movement = StockMovementModel.CreateReceipt(
                sourceRef,
                journal.MutationTime,
                [line],
                StockFactOriginEnum.LegacySynchronized,
                movementId);
        }
        else if (journal.QuantityOut > 0m && journal.QuantityIn == 0m)
        {
            kind = LegacySyncIntentKindEnum.ApplyLegacySynchronizedOutbound;
            movement = StockMovementModel.CreateOutbound(
                sourceRef,
                journal.MutationTime,
                [line],
                StockFactOriginEnum.LegacySynchronized,
                movementId);
        }
        else
        {
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(
                $"JournalInsert '{identity.LegacyJournalId}' has ambiguous quantity direction " +
                $"(in={journal.QuantityIn}, out={journal.QuantityOut}); jenis vocabulary mapping is out of scope."));
        }

        return InterpretStep.Ok(new LegacySyncIntentType(
            kind,
            identity.LegacyJournalId,
            identity.LayananId,
            LegacyRowId: null,
            TargetMovementId: null,
            TargetLayerId: null,
            ProposedMovement: movement,
            SyncIdempotencyKey: LegacyChangeDiscoveryIdentityKeys.BuildJournalKey(scope, journal),
            Explanation: delta.Explanation
                ?? "New legacy journal after baseline → LegacySynchronized movement (new layer establishment for inbound only)."));
    }

    private static InterpretStep InterpretBalanceUpdate(
        IStockLedgerScopeKey scope,
        LegacyDiscoveredDeltaType delta,
        LegacySyncLedgerSnapshot snapshot,
        IReadOnlyDictionary<LegacyBalanceIdentity, LegacyStockBalanceType> balancesByIdentity)
    {
        // P3-S1 classifier stores LegacyRowId in LegacyJournalId for balance deltas.
        if (string.IsNullOrWhiteSpace(delta.LegacyJournalId) || string.IsNullOrWhiteSpace(delta.LayananId))
        {
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(
                "BalanceUpdate delta is missing LegacyRowId (via LegacyJournalId) or LayananId."));
        }

        var identity = new LegacyBalanceIdentity(delta.LayananId.Trim(), delta.LegacyJournalId.Trim());
        if (!snapshot.BalanceAnchors.TryGetValue(identity, out var anchor))
        {
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(
                $"BalanceUpdate for row '{identity.LegacyRowId}' at '{identity.LayananId}' has no Ledger balance anchor."));
        }

        if (!balancesByIdentity.TryGetValue(identity, out var currentBalance))
        {
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(
                $"BalanceUpdate for row '{identity.LegacyRowId}' has no matching current tb_stok row."));
        }

        var quantityDelta = currentBalance.Quantity - anchor.RemainingQuantity;
        if (quantityDelta == 0m)
        {
            // Material fingerprint changed without remaining-qty change (e.g. cost/ED) —
            // quantity-only interpreter cannot invent provenance rewrite; fail closed.
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(
                $"BalanceUpdate for row '{identity.LegacyRowId}' has zero remaining-quantity delta; " +
                "non-quantity material changes require scoped re-derive / reconciliation, not force-balance."));
        }

        var absQty = Math.Abs(quantityDelta);
        var item = BrgObatType.Key(scope.BrgId);
        var receiptSource = ReceiptSourceType.Create(scope.ReceiptSourceId);
        var location = LayananType.Key(identity.LayananId);
        var valuation = UnitValuationType.Create(anchor.UnitCost);
        var layerKey = StockLayerModel.Key(anchor.StockLayerId);
        var effectiveTime = currentBalance.LastMutationTime
            ?? currentBalance.ReceiptTime
            ?? UnknownSyncEffectiveTime;

        StockMovementModel movement;
        var movementId = BuildMovementId(scope, "BAL", identity.LayananId, identity.LegacyRowId);
        if (quantityDelta < 0m)
        {
            var line = StockMovementLineType.Create(
                1,
                item,
                receiptSource,
                location,
                StockMovementDirectionEnum.Outbound,
                absQty,
                valuation,
                StockFactOriginEnum.LegacySynchronized,
                layerKey);
            movement = StockMovementModel.CreateOutbound(
                SourceTransactionReferenceType.Create($"SYNC-BAL|{identity.LegacyRowId}|dec"),
                effectiveTime,
                [line],
                StockFactOriginEnum.LegacySynchronized,
                movementId);
        }
        else
        {
            var line = StockMovementLineType.Create(
                1,
                item,
                receiptSource,
                location,
                StockMovementDirectionEnum.Inbound,
                absQty,
                valuation,
                StockFactOriginEnum.LegacySynchronized,
                layerKey);
            movement = StockMovementModel.CreateReceipt(
                SourceTransactionReferenceType.Create($"SYNC-BAL|{identity.LegacyRowId}|inc"),
                effectiveTime,
                [line],
                StockFactOriginEnum.LegacySynchronized,
                movementId);
        }

        return InterpretStep.Ok(new LegacySyncIntentType(
            LegacySyncIntentKindEnum.AdjustLayerRemainingQuantity,
            LegacyJournalId: null,
            identity.LayananId,
            identity.LegacyRowId,
            TargetMovementId: null,
            TargetLayerId: anchor.StockLayerId,
            ProposedMovement: movement,
            SyncIdempotencyKey: LegacyChangeDiscoveryIdentityKeys.BuildBalanceKey(scope, currentBalance),
            Explanation: delta.Explanation
                ?? $"Balance quantity change {anchor.RemainingQuantity} → {currentBalance.Quantity}; " +
                   $"layer origin '{anchor.LayerOrigin}' preserved (quantity-only movement)."));
    }

    private static InterpretStep InterpretBalanceDelete(
        IStockLedgerScopeKey scope,
        LegacyDiscoveredDeltaType delta,
        LegacySyncLedgerSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(delta.LegacyJournalId) || string.IsNullOrWhiteSpace(delta.LayananId))
        {
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(
                "BalanceDelete delta is missing LegacyRowId (via LegacyJournalId) or LayananId."));
        }

        var identity = new LegacyBalanceIdentity(delta.LayananId.Trim(), delta.LegacyJournalId.Trim());
        if (!snapshot.BalanceAnchors.TryGetValue(identity, out var anchor))
        {
            return InterpretStep.Fail(LegacySyncInterpretationResult.Ambiguous(
                $"BalanceDelete for row '{identity.LegacyRowId}' at '{identity.LayananId}' has no Ledger balance anchor."));
        }

        if (anchor.RemainingQuantity == 0m)
        {
            var omissionKey = string.Join(
                '|',
                "SYNC",
                "STOK",
                scope.BrgId.Trim(),
                scope.ReceiptSourceId.Trim(),
                identity.LayananId,
                identity.LegacyRowId,
                "0",
                "OMISSION");

            return InterpretStep.Ok(new LegacySyncIntentType(
                LegacySyncIntentKindEnum.RepresentationalBalanceOmission,
                LegacyJournalId: null,
                identity.LayananId,
                identity.LegacyRowId,
                TargetMovementId: null,
                TargetLayerId: anchor.StockLayerId,
                ProposedMovement: null,
                SyncIdempotencyKey: omissionKey,
                Explanation: delta.Explanation
                    ?? "Absent tb_stok row with depleted Ledger layer is representational (BR-STL-110/080); no quantity movement."));
        }

        var item = BrgObatType.Key(scope.BrgId);
        var receiptSource = ReceiptSourceType.Create(scope.ReceiptSourceId);
        var location = LayananType.Key(identity.LayananId);
        var valuation = UnitValuationType.Create(anchor.UnitCost);
        var line = StockMovementLineType.Create(
            1,
            item,
            receiptSource,
            location,
            StockMovementDirectionEnum.Outbound,
            anchor.RemainingQuantity,
            valuation,
            StockFactOriginEnum.LegacySynchronized,
            StockLayerModel.Key(anchor.StockLayerId));

        var movement = StockMovementModel.CreateOutbound(
            SourceTransactionReferenceType.Create($"SYNC-BAL|{identity.LegacyRowId}|del"),
            UnknownSyncEffectiveTime,
            [line],
            StockFactOriginEnum.LegacySynchronized,
            BuildMovementId(scope, "BAL", identity.LayananId, identity.LegacyRowId));

        var syntheticBalance = new LegacyStockBalanceType(
            scope.BrgId,
            scope.ReceiptSourceId,
            identity.LayananId,
            Quantity: 0m,
            UnitCost: anchor.UnitCost,
            ExpirationDate: anchor.ExpirationDate,
            Batch: anchor.Batch,
            PurchaseOrderId: null,
            LegacyRowId: identity.LegacyRowId,
            ReceiptTime: null,
            LastMutationTime: null);

        return InterpretStep.Ok(new LegacySyncIntentType(
            LegacySyncIntentKindEnum.AdjustLayerRemainingQuantity,
            LegacyJournalId: null,
            identity.LayananId,
            identity.LegacyRowId,
            TargetMovementId: null,
            TargetLayerId: anchor.StockLayerId,
            ProposedMovement: movement,
            SyncIdempotencyKey: LegacyChangeDiscoveryIdentityKeys.BuildBalanceKey(scope, syntheticBalance),
            Explanation: delta.Explanation
                ?? $"Active layer remaining {anchor.RemainingQuantity} depleted to match absent tb_stok row; " +
                   $"layer origin '{anchor.LayerOrigin}' preserved."));
    }

    private static bool TryBuildJournalLine(
        IStockLedgerScopeKey scope,
        LegacyStockJournalEntryType journal,
        out StockMovementLineType line,
        out string? ambiguity)
    {
        line = null!;
        ambiguity = null;

        if (!TryResolveJournalDirection(journal, out var direction, out var quantity, out ambiguity))
            return false;

        line = StockMovementLineType.Create(
            1,
            BrgObatType.Key(scope.BrgId),
            ReceiptSourceType.Create(scope.ReceiptSourceId),
            LayananType.Key(journal.LayananId),
            direction,
            quantity,
            UnitValuationType.Create(journal.UnitCost),
            StockFactOriginEnum.LegacySynchronized);
        return true;
    }

    /// <summary>
    /// Builds a Domain <see cref="StockMovementModel.Correct"/> line as a compensatory delta
    /// (new legacy quantity − previously represented quantity), not an absolute restatement.
    /// </summary>
    private static bool TryBuildCompensatoryCorrectionLine(
        IStockLedgerScopeKey scope,
        StockMovementModel prior,
        LegacyStockJournalEntryType currentJournal,
        out StockMovementLineType? compensationLine,
        out bool skipQuantityConsequence,
        out string? ambiguity)
    {
        compensationLine = null;
        skipQuantityConsequence = false;
        ambiguity = null;

        if (!TryResolveJournalDirection(
                currentJournal,
                out var journalDirection,
                out var journalQuantity,
                out ambiguity))
        {
            return false;
        }

        if (!TryResolvePriorSingleDirection(
                prior,
                out var priorDirection,
                out var priorQuantity,
                out var templateLine,
                out ambiguity))
        {
            return false;
        }

        if (priorDirection != journalDirection)
        {
            ambiguity =
                $"JournalUpdate '{currentJournal.LegacyJournalId}' changes movement direction " +
                $"from '{priorDirection}' to '{journalDirection}'; cannot build a deterministic " +
                "compensatory correction without inventing business meaning.";
            return false;
        }

        if (!string.Equals(templateLine.BrgId, scope.BrgId, StringComparison.Ordinal)
            || !string.Equals(currentJournal.BrgId.Trim(), scope.BrgId, StringComparison.Ordinal))
        {
            ambiguity =
                $"JournalUpdate '{currentJournal.LegacyJournalId}' has Item mismatch with sync scope; fail closed.";
            return false;
        }

        if (!string.Equals(templateLine.ReceiptSourceId, scope.ReceiptSourceId, StringComparison.Ordinal)
            || !string.Equals(
                currentJournal.ReceiptSourceId.Trim(),
                scope.ReceiptSourceId,
                StringComparison.Ordinal))
        {
            ambiguity =
                $"JournalUpdate '{currentJournal.LegacyJournalId}' changes Receipt Source; " +
                "cannot safely compensate without inventing provenance rewrite.";
            return false;
        }

        if (!string.Equals(
                templateLine.LayananId,
                currentJournal.LayananId.Trim(),
                StringComparison.Ordinal))
        {
            ambiguity =
                $"JournalUpdate '{currentJournal.LegacyJournalId}' changes Stock Location; " +
                "cannot safely compensate without inventing transfer semantics.";
            return false;
        }

        if (templateLine.UnitValuation.AmountPerUnit != currentJournal.UnitCost)
        {
            ambiguity =
                $"JournalUpdate '{currentJournal.LegacyJournalId}' changes unit valuation/cost; " +
                "quantity-only compensatory correction cannot preserve material valuation semantics.";
            return false;
        }

        var quantityDelta = journalQuantity - priorQuantity;
        if (quantityDelta == 0m)
        {
            // No material quantity difference → no duplicate quantity consequence.
            skipQuantityConsequence = true;
            return true;
        }

        // Inventory-signed compensation: inbound increase / outbound decrease → +Inventory.
        var inventoryDelta = journalDirection == StockMovementDirectionEnum.Inbound
            ? quantityDelta
            : -quantityDelta;

        var compensationDirection = inventoryDelta > 0m
            ? StockMovementDirectionEnum.Inbound
            : StockMovementDirectionEnum.Outbound;

        compensationLine = StockMovementLineType.Create(
            1,
            BrgObatType.Key(scope.BrgId),
            ReceiptSourceType.Create(scope.ReceiptSourceId),
            LayananType.Key(currentJournal.LayananId),
            compensationDirection,
            Math.Abs(inventoryDelta),
            UnitValuationType.Create(currentJournal.UnitCost),
            StockFactOriginEnum.LegacySynchronized);
        return true;
    }

    private static bool TryResolveJournalDirection(
        LegacyStockJournalEntryType journal,
        out StockMovementDirectionEnum direction,
        out decimal quantity,
        out string? ambiguity)
    {
        direction = default;
        quantity = 0m;
        ambiguity = null;

        var inbound = journal.QuantityIn > 0m;
        var outbound = journal.QuantityOut > 0m;
        if (inbound == outbound)
        {
            ambiguity =
                $"Journal '{journal.LegacyJournalId}' has ambiguous quantity direction " +
                $"(in={journal.QuantityIn}, out={journal.QuantityOut}).";
            return false;
        }

        direction = inbound
            ? StockMovementDirectionEnum.Inbound
            : StockMovementDirectionEnum.Outbound;
        quantity = inbound ? journal.QuantityIn : journal.QuantityOut;
        if (quantity <= 0m)
        {
            ambiguity = $"Journal '{journal.LegacyJournalId}' has non-positive quantity.";
            return false;
        }

        return true;
    }

    private static bool TryResolvePriorSingleDirection(
        StockMovementModel prior,
        out StockMovementDirectionEnum direction,
        out decimal quantity,
        out StockMovementLineType templateLine,
        out string? ambiguity)
    {
        direction = default;
        quantity = 0m;
        templateLine = null!;
        ambiguity = null;

        if (prior.Lines.Count == 0)
        {
            ambiguity = $"Prior movement '{prior.StockMovementId}' has no lines to compensate.";
            return false;
        }

        var inboundQty = prior.TotalQuantity(StockMovementDirectionEnum.Inbound);
        var outboundQty = prior.TotalQuantity(StockMovementDirectionEnum.Outbound);
        var hasInbound = inboundQty > 0m;
        var hasOutbound = outboundQty > 0m;
        if (hasInbound == hasOutbound)
        {
            ambiguity =
                $"Prior movement '{prior.StockMovementId}' is not a single-direction quantity fact " +
                $"(in={inboundQty}, out={outboundQty}); JournalUpdate compensation fails closed.";
            return false;
        }

        var resolvedDirection = hasInbound
            ? StockMovementDirectionEnum.Inbound
            : StockMovementDirectionEnum.Outbound;
        var resolvedQuantity = hasInbound ? inboundQty : outboundQty;
        var resolvedTemplate = prior.Lines[0];

        // Multi-line same-direction is only safe when material attributes are uniform.
        if (prior.Lines.Any(l =>
                l.Direction != resolvedDirection
                || !string.Equals(l.LayananId, resolvedTemplate.LayananId, StringComparison.Ordinal)
                || !string.Equals(l.ReceiptSourceId, resolvedTemplate.ReceiptSourceId, StringComparison.Ordinal)
                || l.UnitValuation.AmountPerUnit != resolvedTemplate.UnitValuation.AmountPerUnit
                || !string.Equals(l.BrgId, resolvedTemplate.BrgId, StringComparison.Ordinal)))
        {
            ambiguity =
                $"Prior movement '{prior.StockMovementId}' has non-uniform material line attributes; " +
                "cannot build a deterministic compensatory JournalUpdate.";
            return false;
        }

        direction = resolvedDirection;
        quantity = resolvedQuantity;
        templateLine = resolvedTemplate;
        return true;
    }

    private static StockMovementModel? ResolveMovement(
        LegacySyncLedgerSnapshot snapshot,
        LegacySyncJournalAnchor anchor)
    {
        if (anchor.Movement is not null)
            return anchor.Movement;

        return snapshot.Movements.FirstOrDefault(m =>
            string.Equals(m.StockMovementId, anchor.StockMovementId, StringComparison.Ordinal));
    }

    private static string BuildMovementId(
        IStockLedgerScopeKey scope,
        string purpose,
        string layananId,
        string legacyId)
        => LegacyReconstructionBaselineCalculator.DeterministicAccountableId(
            $"SYNC|{purpose}|{scope.BrgId}|{scope.ReceiptSourceId}|{layananId}|{legacyId}");

    private static string BuildVoidIdempotencyKey(
        IStockLedgerScopeKey scope,
        LegacyJournalIdentity identity,
        StockMovementModel prior)
    {
        // Voided journals are absent from current tb_buku; key from scope + identity + prior movement.
        return string.Join(
            '|',
            "SYNC",
            "BUKU",
            scope.BrgId.Trim(),
            scope.ReceiptSourceId.Trim(),
            identity.LayananId,
            identity.LegacyJournalId,
            "VOID",
            prior.StockMovementId);
    }

    private static IReadOnlyList<LegacyDiscoveredDeltaType> OrderDeltas(
        IReadOnlyList<LegacyDiscoveredDeltaType> deltas)
        => deltas
            .OrderBy(d => KindSortOrder(d.Kind))
            .ThenBy(d => d.LayananId ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(d => d.LegacyJournalId ?? string.Empty, StringComparer.Ordinal)
            .ToArray();

    private static int KindSortOrder(LegacyDiscoveredDeltaKindEnum kind)
        => kind switch
        {
            LegacyDiscoveredDeltaKindEnum.JournalVoidDelete => 1,
            LegacyDiscoveredDeltaKindEnum.JournalUpdate => 2,
            LegacyDiscoveredDeltaKindEnum.BalanceDelete => 3,
            LegacyDiscoveredDeltaKindEnum.BalanceUpdate => 4,
            LegacyDiscoveredDeltaKindEnum.JournalInsert => 5,
            LegacyDiscoveredDeltaKindEnum.RequiresScopedReDerive => 6,
            _ => 99
        };

    private static IReadOnlyList<LegacyDiscoveredDeltaType> Deduplicate(
        IReadOnlyList<LegacyDiscoveredDeltaType> ordered)
    {
        var seen = new HashSet<(LegacyDiscoveredDeltaKindEnum Kind, string Journal, string Layanan)>();
        var result = new List<LegacyDiscoveredDeltaType>(ordered.Count);
        foreach (var delta in ordered)
        {
            var key = (
                delta.Kind,
                delta.LegacyJournalId?.Trim() ?? string.Empty,
                delta.LayananId?.Trim() ?? string.Empty);
            if (!seen.Add(key))
                continue;
            result.Add(delta);
        }

        return result;
    }

    private readonly struct InterpretStep
    {
        public LegacySyncIntentType? Intent { get; }
        public LegacySyncInterpretationResult? FailClosed { get; }

        private InterpretStep(LegacySyncIntentType? intent, LegacySyncInterpretationResult? failClosed)
        {
            Intent = intent;
            FailClosed = failClosed;
        }

        public static InterpretStep Ok(LegacySyncIntentType intent) => new(intent, null);
        public static InterpretStep Fail(LegacySyncInterpretationResult result) => new(null, result);
        public static InterpretStep Skip() => new(null, null);
    }
}
