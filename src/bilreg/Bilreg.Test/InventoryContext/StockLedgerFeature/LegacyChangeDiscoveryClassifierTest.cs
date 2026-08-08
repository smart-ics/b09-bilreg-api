using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S1 — Pure classifier unit tests (no SQL / I/O).
/// </summary>
public class LegacyChangeDiscoveryClassifierTest
{
    private static readonly DateTime T1 = new(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T2 = new(2024, 1, 16, 11, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateOnly ExpA = new(2025, 12, 31);

    private static readonly IStockLedgerScopeKey Scope =
        StockLedgerScopeKeyType.Create("BRGP3S1A", "DOP3S1A");

    [Fact]
    public void Unchanged_WhenStoredFingerprintMatchesCurrent()
    {
        var balances = new[] { Balance("LY01", "STK001", 10m) };
        var journals = new[] { Journal("BK001", "LY01", 10m, 0m, T1) };
        var fingerprint = LegacyReconstructionBasisCalculator.Compute(balances, journals);
        var known = BuildKnown(balances, journals);

        var result = Classify(fingerprint, balances, journals, known);

        result.Outcome.Should().Be(LegacyChangeDiscoveryOutcomeEnum.Unchanged);
        result.Deltas.Should().BeEmpty();
    }

    [Fact]
    public void Undeterminable_WhenStoredPositionMissing()
    {
        var balances = new[] { Balance("LY01", "STK001", 10m) };
        var journals = new[] { Journal("BK001", "LY01", 10m, 0m, T1) };
        var fingerprint = LegacyReconstructionBasisCalculator.Compute(balances, journals);

        var result = LegacyChangeDiscoveryClassifier.Classify(
            Scope,
            storedPosition: null,
            fingerprint,
            balances,
            journals,
            LegacyChangeDiscoveryClassifier.ParseKnownIdentities([]));

        result.Outcome.Should().Be(LegacyChangeDiscoveryOutcomeEnum.Undeterminable);
    }

    [Fact]
    public void RequiresScopedReDerive_WhenFingerprintMismatchWithoutLedgerKnownIdentities()
    {
        var baselineBalances = new[] { Balance("LY01", "STK001", 10m) };
        var baselineJournals = new[] { Journal("BK001", "LY01", 10m, 0m, T1) };
        var stored = LegacyReconstructionBasisCalculator.Compute(baselineBalances, baselineJournals);

        var changedBalances = new[] { Balance("LY01", "STK001", 9m) };
        var changedJournals = baselineJournals;
        var current = LegacyReconstructionBasisCalculator.Compute(changedBalances, changedJournals);

        var result = Classify(stored, changedBalances, changedJournals, EmptyKnown());

        result.Outcome.Should().Be(LegacyChangeDiscoveryOutcomeEnum.ChangesDetected);
        result.Deltas.Should().ContainSingle(x => x.Kind == LegacyDiscoveredDeltaKindEnum.RequiresScopedReDerive);
    }

    [Fact]
    public void JournalInsert_IsDetected()
    {
        var baselineBalances = new[] { Balance("LY01", "STK001", 10m) };
        var baselineJournals = new[] { Journal("BK001", "LY01", 10m, 0m, T1) };
        var stored = LegacyReconstructionBasisCalculator.Compute(baselineBalances, baselineJournals);

        var currentJournals = baselineJournals.Append(Journal("BK002", "LY01", 1m, 0m, T2)).ToArray();
        var currentBalances = baselineBalances;
        var known = BuildKnown(baselineBalances, baselineJournals);

        var result = Classify(stored, currentBalances, currentJournals, known);

        result.Deltas.Should().Contain(x => x.Kind == LegacyDiscoveredDeltaKindEnum.JournalInsert
                                            && x.LegacyJournalId == "BK002");
    }

    [Fact]
    public void JournalVoidDelete_IsDetected()
    {
        var baselineBalances = new[] { Balance("LY01", "STK001", 10m) };
        var baselineJournals = new[]
        {
            Journal("BK001", "LY01", 10m, 0m, T1),
            Journal("BK002", "LY01", 0m, 1m, T2)
        };
        var stored = LegacyReconstructionBasisCalculator.Compute(baselineBalances, baselineJournals);
        var known = BuildKnown(baselineBalances, baselineJournals);

        var currentJournals = baselineJournals.Where(x => x.LegacyJournalId != "BK002").ToArray();
        var result = Classify(stored, baselineBalances, currentJournals, known);

        result.Deltas.Should().Contain(x => x.Kind == LegacyDiscoveredDeltaKindEnum.JournalVoidDelete
                                            && x.LegacyJournalId == "BK002");
    }

    [Fact]
    public void JournalUpdate_IsDetected_ForQuantityAndBackdatedMutationTime()
    {
        var baselineBalances = new[] { Balance("LY01", "STK001", 10m) };
        var baselineJournals = new[] { Journal("BK001", "LY01", 10m, 0m, T1) };
        var stored = LegacyReconstructionBasisCalculator.Compute(baselineBalances, baselineJournals);
        var known = BuildKnown(baselineBalances, baselineJournals);

        var updated = Journal("BK001", "LY01", 9m, 0m, new DateTime(2024, 1, 10, 8, 0, 0));
        var result = Classify(stored, baselineBalances, new[] { updated }, known);

        result.Deltas.Should().ContainSingle(x => x.Kind == LegacyDiscoveredDeltaKindEnum.JournalUpdate
                                                  && x.LegacyJournalId == "BK001");
    }

    [Fact]
    public void BalanceDelete_IsDetected()
    {
        var baselineBalances = new[]
        {
            Balance("LY01", "STK001", 10m),
            Balance("LY02", "STK002", 5m)
        };
        var baselineJournals = new[]
        {
            Journal("BK001", "LY01", 10m, 0m, T1),
            Journal("BK002", "LY02", 5m, 0m, T2)
        };
        var stored = LegacyReconstructionBasisCalculator.Compute(baselineBalances, baselineJournals);
        var known = BuildKnown(baselineBalances, baselineJournals);

        var currentBalances = baselineBalances.Where(x => x.LayananId != "LY02").ToArray();
        var result = Classify(stored, currentBalances, baselineJournals, known);

        result.Deltas.Should().Contain(x => x.Kind == LegacyDiscoveredDeltaKindEnum.BalanceDelete
                                            && x.LayananId == "LY02");
    }

    [Fact]
    public void BalanceUpdate_IsDetected()
    {
        var baselineBalances = new[] { Balance("LY01", "STK001", 10m) };
        var baselineJournals = new[] { Journal("BK001", "LY01", 10m, 0m, T1) };
        var stored = LegacyReconstructionBasisCalculator.Compute(baselineBalances, baselineJournals);
        var known = BuildKnown(baselineBalances, baselineJournals);

        var updatedBalance = Balance("LY01", "STK001", 9m);
        var result = Classify(stored, new[] { updatedBalance }, baselineJournals, known);

        result.Deltas.Should().ContainSingle(x => x.Kind == LegacyDiscoveredDeltaKindEnum.BalanceUpdate);
    }

    [Fact]
    public void Repost_IsDetectedAsVoidDeletePlusInsert()
    {
        var baselineBalances = new[] { Balance("LY01", "STK001", 10m) };
        var baselineJournals = new[] { Journal("BK001", "LY01", 10m, 0m, T1) };
        var stored = LegacyReconstructionBasisCalculator.Compute(baselineBalances, baselineJournals);
        var known = BuildKnown(baselineBalances, baselineJournals);

        var reposted = Journal("BK999", "LY01", 10m, 0m, T2);
        var result = Classify(stored, baselineBalances, new[] { reposted }, known);

        result.Deltas.Should().Contain(x => x.Kind == LegacyDiscoveredDeltaKindEnum.JournalVoidDelete
                                            && x.LegacyJournalId == "BK001");
        result.Deltas.Should().Contain(x => x.Kind == LegacyDiscoveredDeltaKindEnum.JournalInsert
                                            && x.LegacyJournalId == "BK999");
    }

    [Fact]
    public void DuplicateJournalIdentity_IsUndeterminable()
    {
        var baselineBalances = new[] { Balance("LY01", "STK001", 10m) };
        var baselineJournals = new[]
        {
            Journal("BK001", "LY01", 10m, 0m, T1),
            Journal("BK001", "LY01", 1m, 0m, T2)
        };
        var stored = LegacyReconstructionBasisCalculator.Compute(baselineBalances, new[] { baselineJournals[0] });
        var known = BuildKnown(baselineBalances, new[] { baselineJournals[0] });

        var result = Classify(stored, baselineBalances, baselineJournals, known);

        result.Outcome.Should().Be(LegacyChangeDiscoveryOutcomeEnum.Undeterminable);
    }

    private static LegacyChangeDiscoveryResult Classify(
        SynchronizationPositionType stored,
        IReadOnlyList<LegacyStockBalanceType> balances,
        IReadOnlyList<LegacyStockJournalEntryType> journals,
        LegacyChangeDiscoveryClassifier.KnownLegacyIdentitySet known)
    {
        var current = LegacyReconstructionBasisCalculator.Compute(balances, journals);
        return LegacyChangeDiscoveryClassifier.Classify(Scope, stored, current, balances, journals, known);
    }

    private static LegacyChangeDiscoveryClassifier.KnownLegacyIdentitySet EmptyKnown()
        => LegacyChangeDiscoveryClassifier.ParseKnownIdentities([]);

    private static LegacyChangeDiscoveryClassifier.KnownLegacyIdentitySet BuildKnown(
        IReadOnlyList<LegacyStockBalanceType> balances,
        IReadOnlyList<LegacyStockJournalEntryType> journals)
    {
        var keys = journals
            .Select(j => LegacyChangeDiscoveryIdentityKeys.BuildJournalKey(Scope, j))
            .Concat(balances
                .Where(b => !string.IsNullOrWhiteSpace(b.LegacyRowId))
                .Select(b => LegacyChangeDiscoveryIdentityKeys.BuildBalanceKey(Scope, b)));
        return LegacyChangeDiscoveryClassifier.ParseKnownIdentities(keys);
    }

    private static LegacyStockBalanceType Balance(string layananId, string legacyRowId, decimal qty)
        => new(
            Scope.BrgId,
            Scope.ReceiptSourceId,
            layananId,
            qty,
            1000m,
            ExpA,
            "B1",
            PurchaseOrderId: null,
            legacyRowId,
            ReceiptTime: T1,
            LastMutationTime: T1);

    private static LegacyStockJournalEntryType Journal(
        string journalId,
        string layananId,
        decimal qtyIn,
        decimal qtyOut,
        DateTime mutationTime)
        => new(
            journalId,
            Scope.BrgId,
            Scope.ReceiptSourceId,
            layananId,
            qtyIn,
            qtyOut,
            1000m,
            ExpA,
            "B1",
            MutationKindId: "DO",
            MutationTransactionId: journalId,
            mutationTime,
            PurchaseOrderId: null);
}
