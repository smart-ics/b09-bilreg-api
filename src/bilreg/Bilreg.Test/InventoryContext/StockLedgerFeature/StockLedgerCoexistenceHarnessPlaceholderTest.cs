namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// G-23 coexistence harness scaffolding (Phase 1 markers only).
/// Full Legacy↔New scenarios belong to later phases once live adapters exist.
/// </summary>
public class StockLedgerCoexistenceHarnessPlaceholderTest
{
    [Fact(Skip = "G-23 scaffolding — Legacy→New synchronization scenario requires Phase 3 discovery + sync.")]
    public void LegacyToNew_SynchronizesLegacyOriginatedChange()
    {
        Assert.Fail("Not implemented: Legacy→New coexistence scenario.");
    }

    [Fact(Skip = "G-23 scaffolding — New→Legacy visibility requires Phase 4 live Legacy Compatibility Writer.")]
    public void NewToLegacy_ReceiptVisibleInLegacyAuthority()
    {
        Assert.Fail("Not implemented: New→Legacy coexistence scenario.");
    }

    [Fact(Skip = "G-23 scaffolding — alternating writers requires Phase 3/4 mixed-writer enablement.")]
    public void AlternatingWriters_RemainReconcileable()
    {
        Assert.Fail("Not implemented: alternating writers scenario.");
    }

    [Fact(Skip = "G-23 scaffolding — depleted-layer intentional difference covered after reconstruction (Phase 2+).")]
    public void DepletedLayer_IntentionalLegacyDifference_IsReconciledAsRepresentational()
    {
        Assert.Fail("Not implemented: depleted-layer difference scenario.");
    }

    [Fact(Skip = "G-23 scaffolding — real mismatch classification requires Phase 3/8 reconciliation behavior.")]
    public void RealMismatch_IsClassifiedAndSurfaced()
    {
        Assert.Fail("Not implemented: real mismatch scenario.");
    }

    [Fact(Skip = "G-23 scaffolding — duplicate sync batch covered when Phase 3 sync UoW exists.")]
    public void DuplicateSyncBatch_IsIdempotent()
    {
        Assert.Fail("Not implemented: duplicate sync batch scenario.");
    }

    [Fact(Skip = "G-23 scaffolding — concurrent outbound requires Phase 5 capability + OCC hardening.")]
    public void ConcurrentOutbound_RespectsWriteConsistencyScope()
    {
        Assert.Fail("Not implemented: concurrent outbound scenario.");
    }

    [Fact(Skip = "G-23 scaffolding — partial-failure rollback across live legacy+Ledger is Phase 4+ (Ledger-only proven in P1-S8).")]
    public void PartialFailure_LegacyAndLedger_RollBackTogether()
    {
        Assert.Fail("Not implemented: live legacy+Ledger partial-failure scenario.");
    }
}
