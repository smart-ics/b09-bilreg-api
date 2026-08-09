using System.Data.SqlClient;
using System.Reflection;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Test.InventoryContext.StockLedgerFeature.Fakes;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P5-S3 — Native Stock Transfer consequence UseCase against disposable <c>devTest</c>.
/// Never writes <c>HOSPITAL_HPL</c>.
/// </summary>
[Collection("StockLedgerP3S4")]
public class PostStockTransferStockConsequenceHandlerTest
{
    private static readonly DateTime BusinessTime = new(2026, 8, 8, 14, 0, 0);
    private static readonly DateTime ProcessedAt = new(2026, 8, 8, 14, 5, 0);
    private static readonly DateOnly Expiration = new(2027, 6, 30);
    private static readonly DateOnly ExpirationAlt = new(2027, 9, 30);
    private const decimal UnitCost = 1500.50m;

    [Fact]
    public async Task HappyPath_CapabilityEnabled_NativeOriginAndConservedLegacyOutIn()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("HP");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            await SeedSourceAndBaselineAsync(harness, ids, qty: 10m);

            var result = await harness.Handler.Handle(BuildCommand(ids, quantity: 7m), default);

            result.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.Committed);
            result.StockMovementId.Should().NotBeNullOrWhiteSpace();
            result.PrimaryScopeState!.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructed);
            result.PrimaryScopeState.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
            result.SynchronizationPosition!.AlgorithmVersion.Should().Be(
                LegacyReconstructionBasisCalculator.AlgorithmVersion);

            var movement = harness.Repos.Movement
                .LoadEntity(StockMovementModel.Key(result.StockMovementId!));
            movement.HasValue.Should().BeTrue();
            movement.Value.Origin.Should().Be(StockFactOriginEnum.Native);
            movement.Value.MovementKind.Should().Be(StockMovementKindEnum.Transfer);
            movement.Value.TotalQuantity(StockMovementDirectionEnum.Outbound).Should().Be(7m);
            movement.Value.TotalQuantity(StockMovementDirectionEnum.Inbound).Should().Be(7m);
            movement.Value.Lines.Should().OnlyContain(l => l.Origin == StockFactOriginEnum.Native);
            movement.Value.Lines.Should().OnlyContain(l => l.ReceiptSourceId == ids.DoId);

            var sourcePos = harness.Repos.Position.LoadEntity(
                StockWriteScopeKeyType.Create(ids.BrgId, ids.DoId, ids.SourceLocationId));
            sourcePos.HasValue.Should().BeTrue();
            sourcePos.Value.TotalRemainingQuantity.Should().Be(3m);

            var destPos = harness.Repos.Position.LoadEntity(
                StockWriteScopeKeyType.Create(ids.BrgId, ids.DoId, ids.DestLocationId));
            destPos.HasValue.Should().BeTrue();
            destPos.Value.TotalRemainingQuantity.Should().Be(7m);
            destPos.Value.Layers.Should().OnlyContain(l => l.ReceiptSourceId == ids.DoId);
            destPos.Value.Layers.Should().OnlyContain(l => l.Origin == StockFactOriginEnum.Native);
            destPos.Value.Layers.Single().EffectiveReceiptTime
                .Should().Be(sourcePos.Value.Layers.Single().EffectiveReceiptTime);

            var priorKey = TransferConsequenceIdempotency.BuildSourceConsequenceKey(ids.MtId);
            priorKey.Should().Be($"MT|{ids.MtId}");
            var idem = harness.Repos.Idempotency.LoadByBusinessKey(
                StockSourceIdempotencyModel.BusinessKey(
                    StockSourceIdempotencyKindEnum.SourceConsequence,
                    priorKey));
            idem.HasValue.Should().BeTrue();
            idem.Value.IdempotencyKey.Should().Be(priorKey);
            idem.Value.IdempotencyKey.Should().NotContain(ids.BrgId);

            var balances = harness.LegacyRead.ListCurrentBalances(ids.Scope);
            balances.Where(b => b.LayananId == ids.SourceLocationId).Sum(b => b.Quantity).Should().Be(3m);
            balances.Where(b => b.LayananId == ids.DestLocationId).Sum(b => b.Quantity).Should().Be(7m);

            var journals = harness.LegacyRead.ListJournalEntries(ids.Scope);
            journals.Should().Contain(j => j.MutationKindId == "MT_OUT" && j.QuantityOut == 7m);
            journals.Should().Contain(j => j.MutationKindId == "MT_IN" && j.QuantityIn == 7m);
            journals.Where(j => j.MutationKindId is "MT_OUT" or "MT_IN")
                .Should().OnlyContain(j => j.MutationTransactionId == ids.MtId);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task CapabilityDisabled_ReturnsWithoutWrites()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("DI");
        Cleanup(ids);

        try
        {
            var seedHarness = CreateLiveHarness(enabled: true);
            await SeedSourceAndBaselineAsync(seedHarness, ids, qty: 10m);
            var before = CountLegacyRows(ids);

            var disabled = CreateLiveHarness(enabled: false);
            var result = await disabled.Handler.Handle(BuildCommand(ids, quantity: 5m), default);

            result.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.Disabled);
            result.StockMovementId.Should().BeNull();
            CountLedgerTransferMovements(ids).Should().Be(0);
            CountLegacyRows(ids).Should().Be(before);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task DuplicateSourceResponsibility_IsQuantityNeutral()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("DP");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            await SeedSourceAndBaselineAsync(harness, ids, qty: 10m);

            var first = await harness.Handler.Handle(BuildCommand(ids, quantity: 4m), default);
            first.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.Committed);

            var second = await harness.Handler.Handle(BuildCommand(ids, quantity: 4m), default);
            second.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.AlreadyCommitted);
            second.StockMovementId.Should().Be(first.StockMovementId);

            var sourcePos = harness.Repos.Position.LoadEntity(
                StockWriteScopeKeyType.Create(ids.BrgId, ids.DoId, ids.SourceLocationId));
            sourcePos.Value.TotalRemainingQuantity.Should().Be(6m);
            CountLedgerTransferMovements(ids).Should().Be(1);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task MultiItem_ReorderedLines_SameMutasi_IsQuantityNeutral()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var (idsA, idsB) = CreateMultiItemCaseIds("RO");
        CleanupMultiItem(idsA, idsB);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            await SeedSourceAndBaselineAsync(harness, idsA, qty: 10m);
            await SeedSourceAndBaselineAsync(harness, idsB, qty: 10m);

            var first = await harness.Handler.Handle(
                BuildMultiItemCommand(idsA, qtyA: 3m, idsB, qtyB: 4m, aFirst: true),
                default);
            first.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.Committed);

            var second = await harness.Handler.Handle(
                BuildMultiItemCommand(idsA, qtyA: 3m, idsB, qtyB: 4m, aFirst: false),
                default);
            second.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.AlreadyCommitted);
            second.StockMovementId.Should().Be(first.StockMovementId);

            CountLedgerTransferMovements(idsA).Should().Be(1);
            AssertSourceRemaining(harness, idsA, 7m);
            AssertSourceRemaining(harness, idsB, 6m);
            AssertDestRemaining(harness, idsA, 3m);
            AssertDestRemaining(harness, idsB, 4m);

            var key = TransferConsequenceIdempotency.BuildSourceConsequenceKey(idsA.MtId);
            key.Should().Be($"MT|{idsA.MtId}");
            var idem = harness.Repos.Idempotency.LoadByBusinessKey(
                StockSourceIdempotencyModel.BusinessKey(
                    StockSourceIdempotencyKindEnum.SourceConsequence,
                    key));
            idem.HasValue.Should().BeTrue();
            idem.Value.IdempotencyKey.Should().Be(key);
        }
        finally
        {
            CleanupMultiItem(idsA, idsB);
        }
    }

    [Fact]
    public async Task MultiItem_PartialRetry_SameMutasi_IsQuantityNeutral()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var (idsA, idsB) = CreateMultiItemCaseIds("PR");
        CleanupMultiItem(idsA, idsB);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            await SeedSourceAndBaselineAsync(harness, idsA, qty: 10m);
            await SeedSourceAndBaselineAsync(harness, idsB, qty: 10m);

            var first = await harness.Handler.Handle(
                BuildMultiItemCommand(idsA, qtyA: 3m, idsB, qtyB: 4m, aFirst: true),
                default);
            first.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.Committed);

            // Partial retry: only ItemB under the same MT id — must not open a second consequence.
            var partial = await harness.Handler.Handle(
                new PostStockTransferStockConsequenceCommand(
                    idsA.MtId,
                    idsA.SourceLocationId,
                    idsA.DestLocationId,
                    BusinessTime,
                    [new StockTransferLineFact(1, idsB.BrgId, 4m)],
                    ProcessedAt),
                default);
            partial.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.AlreadyCommitted);
            partial.StockMovementId.Should().Be(first.StockMovementId);

            CountLedgerTransferMovements(idsA).Should().Be(1);
            AssertSourceRemaining(harness, idsA, 7m);
            AssertSourceRemaining(harness, idsB, 6m);
            AssertDestRemaining(harness, idsA, 3m);
            AssertDestRemaining(harness, idsB, 4m);

            harness.LegacyRead.ListJournalEntries(idsB.Scope)
                .Count(j => j.MutationKindId is "MT_OUT" or "MT_IN")
                .Should().Be(2);
        }
        finally
        {
            CleanupMultiItem(idsA, idsB);
        }
    }

    [Fact]
    public async Task MultiLayerSingleDo_TransferConserved()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("ML");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            SeedSourceReceipt(ids, qty: 5m, unitCost: UnitCost, expiration: Expiration, batch: "ML-A",
                mutationTime: BusinessTime.AddDays(-10));
            SeedSourceReceipt(ids, qty: 5m, unitCost: UnitCost, expiration: ExpirationAlt, batch: "ML-B",
                mutationTime: BusinessTime.AddDays(-5));
            await ReconstructAndSyncAsync(harness, ids.Scope);

            var result = await harness.Handler.Handle(BuildCommand(ids, quantity: 8m), default);
            result.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.Committed);

            var movement = harness.Repos.Movement
                .LoadEntity(StockMovementModel.Key(result.StockMovementId!)).Value;
            movement.Lines.Count(l => l.Direction == StockMovementDirectionEnum.Outbound)
                .Should().BeGreaterThanOrEqualTo(2);

            var balances = harness.LegacyRead.ListCurrentBalances(ids.Scope);
            balances.Where(b => b.LayananId == ids.SourceLocationId).Sum(b => b.Quantity).Should().Be(2m);
            balances.Where(b => b.LayananId == ids.DestLocationId).Sum(b => b.Quantity).Should().Be(8m);

            var journals = harness.LegacyRead.ListJournalEntries(ids.Scope);
            journals.Count(j => j.MutationKindId == "MT_OUT").Should().BeGreaterThanOrEqualTo(2);
            journals.Count(j => j.MutationKindId == "MT_IN").Should().BeGreaterThanOrEqualTo(2);
            journals.Where(j => j.MutationKindId == "MT_OUT").Sum(j => j.QuantityOut).Should().Be(8m);
            journals.Where(j => j.MutationKindId == "MT_IN").Sum(j => j.QuantityIn).Should().Be(8m);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task ExistingDestination_MultiLayerIncrease_PreservesOcc()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("EI");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            SeedSourceReceipt(ids, qty: 5m, unitCost: UnitCost, expiration: Expiration, batch: "EI-A",
                mutationTime: BusinessTime.AddDays(-10));
            SeedSourceReceipt(ids, qty: 5m, unitCost: UnitCost, expiration: ExpirationAlt, batch: "EI-B",
                mutationTime: BusinessTime.AddDays(-5));
            SeedSourceReceipt(ids, qty: 3m, unitCost: UnitCost, expiration: Expiration, batch: "EI-D",
                mutationTime: BusinessTime.AddDays(-3),
                locationId: ids.DestLocationId);
            await ReconstructAndSyncAsync(harness, ids.Scope);

            var destKey = StockWriteScopeKeyType.Create(ids.BrgId, ids.DoId, ids.DestLocationId);
            var destBefore = harness.Repos.Position.LoadEntity(destKey);
            destBefore.HasValue.Should().BeTrue();
            var priorVersion = destBefore.Value.Version;
            var priorRemaining = destBefore.Value.TotalRemainingQuantity;
            priorRemaining.Should().Be(3m);
            // Reconstruction establishes Positions at Version 0; increase must still be +1.

            // Multi-layer increase into the already-persisted destination write-scope
            // (must be Version + 1, not + N).
            var result = await harness.Handler.Handle(BuildCommand(ids, quantity: 8m), default);
            result.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.Committed);

            var destAfter = harness.Repos.Position.LoadEntity(destKey);
            destAfter.HasValue.Should().BeTrue();
            destAfter.Value.TotalRemainingQuantity.Should().Be(priorRemaining + 8m);
            destAfter.Value.Version.Should().Be(priorVersion + 1);
            destAfter.Value.Layers.Count(l => l.RemainingQuantity > 0)
                .Should().BeGreaterThanOrEqualTo(3);

            var movement = harness.Repos.Movement
                .LoadEntity(StockMovementModel.Key(result.StockMovementId!)).Value;
            movement.Lines.Count(l => l.Direction == StockMovementDirectionEnum.Inbound)
                .Should().BeGreaterThanOrEqualTo(2);

            var journals = harness.LegacyRead.ListJournalEntries(ids.Scope);
            journals.Where(j => j.MutationKindId == "MT_OUT" && j.MutationTransactionId == ids.MtId)
                .Sum(j => j.QuantityOut).Should().Be(8m);
            journals.Where(j => j.MutationKindId == "MT_IN" && j.MutationTransactionId == ids.MtId)
                .Sum(j => j.QuantityIn).Should().Be(8m);

            AssertSourceRemaining(harness, ids, 2m);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task MultiDoLine_TransferConserved()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("MD");
        var do2 = $"D2{ids.DoId}"[..10];
        var scope2 = StockLedgerScopeKeyType.Create(ids.BrgId, do2);
        Cleanup(ids);
        CleanupScope(ids.BrgId, do2, ids.MtId, ids.SourceTxId);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            SeedSourceReceipt(ids, qty: 5m, unitCost: UnitCost, expiration: Expiration, batch: "MD-1",
                mutationTime: BusinessTime.AddDays(-10));
            SeedSourceReceipt(
                ids with { DoId = do2, Scope = scope2, PoId = $"P2{ids.PoId}"[..10] },
                qty: 5m,
                unitCost: 1600m,
                expiration: Expiration,
                batch: "MD-2",
                mutationTime: BusinessTime.AddDays(-5));
            await ReconstructAndSyncAsync(harness, ids.Scope);
            await ReconstructAndSyncAsync(harness, scope2);

            var result = await harness.Handler.Handle(BuildCommand(ids, quantity: 7m), default);
            result.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.Committed);

            var movement = harness.Repos.Movement
                .LoadEntity(StockMovementModel.Key(result.StockMovementId!)).Value;
            movement.Lines.Select(l => l.ReceiptSourceId).Distinct().Should().HaveCount(2);
            movement.TotalQuantity(StockMovementDirectionEnum.Outbound).Should().Be(7m);

            var scope1 = harness.Repos.Scope.LoadEntity(ids.Scope);
            var scope2Loaded = harness.Repos.Scope.LoadEntity(scope2);
            scope1.HasValue.Should().BeTrue();
            scope2Loaded.HasValue.Should().BeTrue();
            scope1.Value.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
            scope2Loaded.Value.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
            scope1.Value.SynchronizationPosition!.AlgorithmVersion.Should().Be(
                LegacyReconstructionBasisCalculator.AlgorithmVersion);
            scope2Loaded.Value.SynchronizationPosition!.AlgorithmVersion.Should().Be(
                LegacyReconstructionBasisCalculator.AlgorithmVersion);

            var bal1 = harness.LegacyRead.ListCurrentBalances(ids.Scope);
            var bal2 = harness.LegacyRead.ListCurrentBalances(scope2);
            (bal1.Sum(b => b.Quantity) + bal2.Sum(b => b.Quantity)).Should().Be(10m);
            bal1.Concat(bal2).Where(b => b.LayananId == ids.DestLocationId).Sum(b => b.Quantity)
                .Should().Be(7m);
        }
        finally
        {
            Cleanup(ids);
            CleanupScope(ids.BrgId, do2, ids.MtId, ids.SourceTxId);
        }
    }

    [Fact]
    public async Task ExplicitExpirationDateFilter_Path()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("ED");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            SeedSourceReceipt(ids, qty: 6m, unitCost: UnitCost, expiration: Expiration, batch: "ED-A",
                mutationTime: BusinessTime.AddDays(-10));
            SeedSourceReceipt(ids, qty: 6m, unitCost: UnitCost, expiration: ExpirationAlt, batch: "ED-B",
                mutationTime: BusinessTime.AddDays(-5));
            await ReconstructAndSyncAsync(harness, ids.Scope);

            var result = await harness.Handler.Handle(
                BuildCommand(ids, quantity: 4m, expirationDate: Expiration), default);

            result.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.Committed);

            var destBalances = harness.LegacyRead.ListCurrentBalances(ids.Scope)
                .Where(b => b.LayananId == ids.DestLocationId)
                .ToList();
            destBalances.Should().OnlyContain(b => b.ExpirationDate == Expiration);
            destBalances.Sum(b => b.Quantity).Should().Be(4m);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task InsufficientStock_FailClosed()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("IN");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            await SeedSourceAndBaselineAsync(harness, ids, qty: 3m);

            var result = await harness.Handler.Handle(BuildCommand(ids, quantity: 10m), default);

            result.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.InsufficientStock);
            result.StockMovementId.Should().BeNull();
            CountLedgerTransferMovements(ids).Should().Be(0);
            harness.LegacyRead.ListJournalEntries(ids.Scope)
                .Should().NotContain(j => j.MutationKindId == "MT_OUT" || j.MutationKindId == "MT_IN");
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task StaleScope_FailClosed()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("ST");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            await SeedSourceAndBaselineAsync(harness, ids, qty: 10m);

            // Force SynchronizationRequired so Freshness Gate (via orchestrator) fails closed.
            var current = harness.Repos.Scope.LoadEntity(ids.Scope).Value;
            var stale = current.RequireSynchronization();
            harness.Repos.Scope.SaveChanges(stale);

            var result = await harness.Handler.Handle(BuildCommand(ids, quantity: 5m), default);

            result.Outcome.Should().BeOneOf(
                PostStockTransferStockConsequenceOutcomeEnum.StaleOrNotCurrent,
                PostStockTransferStockConsequenceOutcomeEnum.Inconsistent);
            result.StockMovementId.Should().BeNull();
            CountLedgerTransferMovements(ids).Should().Be(0);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task NoIsAuthoritative_OnScopeOrMovement()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("NA");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            await SeedSourceAndBaselineAsync(harness, ids, qty: 5m);
            var result = await harness.Handler.Handle(BuildCommand(ids, quantity: 2m), default);
            result.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.Committed);

            typeof(StockLedgerScopeStateModel).GetProperty(
                    "IsAuthoritative",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Should().BeNull();
            typeof(StockMovementModel).GetProperty(
                    "IsAuthoritative",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Should().BeNull();
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task SameAttributeMultiBalance_OutboundUsesBoundFifoLayerRow()
    {
        // Review Major: repeated MT_IN with shared DO/HPP/ED/batch creates multiple ST rows.
        // Outbound qty equal to the non-FIFO-first row must deplete the FIFO-selected layer's
        // bound LegacyRowId — not the exact-qty peer.
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var source = CreateCaseIds("WR");
        var hop = CreateCaseIds("WH");
        Cleanup(source);
        Cleanup(hop);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            await SeedSourceAndBaselineAsync(harness, source, qty: 8m);

            var firstIn = await harness.Handler.Handle(BuildCommand(source, quantity: 5m), default);
            firstIn.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.Committed);
            var secondMtId = $"MT2{Ulid.NewUlid()}"[..10];
            var secondIn = await harness.Handler.Handle(
                new PostStockTransferStockConsequenceCommand(
                    SourceTransactionId: secondMtId,
                    SourceLocationId: source.SourceLocationId,
                    DestinationLocationId: source.DestLocationId,
                    EffectiveBusinessTime: BusinessTime.AddMinutes(1),
                    Lines: [new StockTransferLineFact(1, source.BrgId, 3m)],
                    ProcessedAt: ProcessedAt.AddMinutes(1)),
                default);
            secondIn.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.Committed);

            var destWriteScope = StockWriteScopeKeyType.Create(
                source.BrgId, source.DoId, source.DestLocationId);
            var destPos = harness.Repos.Position.LoadEntity(destWriteScope).Value;
            destPos.Layers.Should().HaveCount(2);
            var fifoFirst = destPos.Layers
                .OrderBy(l => l.EffectiveReceiptTime)
                .ThenBy(l => l.StockLayerId, StringComparer.Ordinal)
                .First();
            var fifoFirstBinding = harness.Repos.Binding.LoadByStockLayerId(fifoFirst.StockLayerId);
            fifoFirstBinding.HasValue.Should().BeTrue();
            var expectedLegacyRowId = fifoFirstBinding.Value.LegacyRowId;

            var balancesBefore = harness.LegacyRead.ListCurrentBalances(source.Scope)
                .Where(b => b.LayananId == source.DestLocationId)
                .ToList();
            balancesBefore.Should().HaveCount(2);
            balancesBefore.Select(b => b.Quantity).Should().BeEquivalentTo([5m, 3m]);
            var exactQtyPeerId = balancesBefore.Single(b => b.Quantity == 3m).LegacyRowId!;
            exactQtyPeerId.Should().NotBe(expectedLegacyRowId);

            var outbound = await harness.Handler.Handle(
                new PostStockTransferStockConsequenceCommand(
                    SourceTransactionId: hop.MtId,
                    SourceLocationId: source.DestLocationId,
                    DestinationLocationId: hop.DestLocationId,
                    EffectiveBusinessTime: BusinessTime.AddMinutes(2),
                    Lines: [new StockTransferLineFact(1, source.BrgId, 3m)],
                    ProcessedAt: ProcessedAt.AddMinutes(2)),
                default);
            outbound.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.Committed);

            var balancesAfter = harness.LegacyRead.ListCurrentBalances(source.Scope)
                .Where(b => b.LayananId == source.DestLocationId)
                .ToList();
            // Correct path: FIFO-first row (qty 5) reduced to 2; exact-qty peer (qty 3) untouched.
            balancesAfter.Should().HaveCount(2);
            balancesAfter.Should().Contain(b =>
                b.LegacyRowId == expectedLegacyRowId && b.Quantity == 2m);
            balancesAfter.Should().Contain(b =>
                b.LegacyRowId == exactQtyPeerId && b.Quantity == 3m);
        }
        finally
        {
            Cleanup(source);
            Cleanup(hop);
        }
    }

    [Fact]
    public async Task AmbiguousUnboundSameAttributeBalances_FailClosedInconsistent()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var source = CreateCaseIds("AM");
        var hop = CreateCaseIds("AH");
        Cleanup(source);
        Cleanup(hop);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            await SeedSourceAndBaselineAsync(harness, source, qty: 8m);

            (await harness.Handler.Handle(BuildCommand(source, quantity: 5m), default))
                .Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.Committed);
            var secondMtId = $"MT2{Ulid.NewUlid()}"[..10];
            (await harness.Handler.Handle(
                    new PostStockTransferStockConsequenceCommand(
                        SourceTransactionId: secondMtId,
                        SourceLocationId: source.SourceLocationId,
                        DestinationLocationId: source.DestLocationId,
                        EffectiveBusinessTime: BusinessTime.AddMinutes(1),
                        Lines: [new StockTransferLineFact(1, source.BrgId, 3m)],
                        ProcessedAt: ProcessedAt.AddMinutes(1)),
                    default))
                .Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.Committed);

            // Remove durable bindings so resolver must face same-attribute multi-candidate ambiguity.
            using (var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value)))
            {
                conn.Open();
                conn.Execute(
                    """
                    DELETE FROM BILRG_StokLayerLegacyBinding
                    WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId AND LayananId = @LayananId
                    """,
                    new
                    {
                        source.BrgId,
                        source.DoId,
                        LayananId = source.DestLocationId
                    });
            }

            var outbound = await harness.Handler.Handle(
                new PostStockTransferStockConsequenceCommand(
                    SourceTransactionId: hop.MtId,
                    SourceLocationId: source.DestLocationId,
                    DestinationLocationId: hop.DestLocationId,
                    EffectiveBusinessTime: BusinessTime.AddMinutes(2),
                    Lines: [new StockTransferLineFact(1, source.BrgId, 3m)],
                    ProcessedAt: ProcessedAt.AddMinutes(2)),
                default);

            outbound.Outcome.Should().Be(PostStockTransferStockConsequenceOutcomeEnum.Inconsistent);
            outbound.StockMovementId.Should().BeNull();
            CountLedgerTransferMovements(hop).Should().Be(0);
            harness.LegacyRead.ListCurrentBalances(source.Scope)
                .Where(b => b.LayananId == source.DestLocationId)
                .Sum(b => b.Quantity)
                .Should().Be(8m);
        }
        finally
        {
            Cleanup(source);
            Cleanup(hop);
        }
    }

    #region Helpers

    private static PostStockTransferStockConsequenceCommand BuildCommand(
        CaseIds ids,
        decimal quantity,
        DateOnly? expirationDate = null)
        => new(
            ids.MtId,
            ids.SourceLocationId,
            ids.DestLocationId,
            BusinessTime,
            [
                new StockTransferLineFact(
                    LineNumber: 1,
                    BrgId: ids.BrgId,
                    Quantity: quantity,
                    ExpirationDate: expirationDate)
            ],
            ProcessedAt);

    private static PostStockTransferStockConsequenceCommand BuildMultiItemCommand(
        CaseIds idsA,
        decimal qtyA,
        CaseIds idsB,
        decimal qtyB,
        bool aFirst)
    {
        StockTransferLineFact lineA = new(aFirst ? 1 : 2, idsA.BrgId, qtyA);
        StockTransferLineFact lineB = new(aFirst ? 2 : 1, idsB.BrgId, qtyB);
        return new(
            idsA.MtId,
            idsA.SourceLocationId,
            idsA.DestLocationId,
            BusinessTime,
            aFirst ? [lineA, lineB] : [lineB, lineA],
            ProcessedAt);
    }

    private static void AssertSourceRemaining(Harness harness, CaseIds ids, decimal expected)
    {
        var pos = harness.Repos.Position.LoadEntity(
            StockWriteScopeKeyType.Create(ids.BrgId, ids.DoId, ids.SourceLocationId));
        pos.HasValue.Should().BeTrue();
        pos.Value.TotalRemainingQuantity.Should().Be(expected);
    }

    private static void AssertDestRemaining(Harness harness, CaseIds ids, decimal expected)
    {
        var pos = harness.Repos.Position.LoadEntity(
            StockWriteScopeKeyType.Create(ids.BrgId, ids.DoId, ids.DestLocationId));
        pos.HasValue.Should().BeTrue();
        pos.Value.TotalRemainingQuantity.Should().Be(expected);
    }

    private static async Task SeedSourceAndBaselineAsync(Harness harness, CaseIds ids, decimal qty)
    {
        SeedSourceReceipt(ids, qty, UnitCost, Expiration, "P5S3-BATCH", BusinessTime.AddDays(-1));
        await ReconstructAndSyncAsync(harness, ids.Scope);
    }

    private static void SeedSourceReceipt(
        CaseIds ids,
        decimal qty,
        decimal unitCost,
        DateOnly expiration,
        string batch,
        DateTime mutationTime,
        string? locationId = null,
        string? legacyRowId = null,
        string? legacyJournalId = null)
    {
        var layananId = locationId ?? ids.SourceLocationId;
        // Pre-assign BK/ST when omitted — NewLegacyCompact collisions flake multi-seed fixtures (FQ-02).
        // Use Ulid *tail* (random bits); head is timestamp and collides when truncated to 10 chars.
        if (string.IsNullOrWhiteSpace(legacyRowId) || string.IsNullOrWhiteSpace(legacyJournalId))
        {
            var suffix = Ulid.NewUlid().ToString();
            legacyRowId ??= $"ST{suffix[^8..]}";
            legacyJournalId ??= $"BK{suffix[^8..]}";
        }

        var writer = new LegacyCompatibilityWriterPort(ConnStringHelper.GetTestEnv());
        var request = new LegacyCompatibilityWriteRequest(
            SourceTransactionReferenceType.Key($"SEED-{ids.SourceTxId}-{batch}"),
            StockMovementKindEnum.Receipt,
            ids.Scope,
            [
                new LegacyCompatibilityBalanceMutationType(
                    LegacyBalanceMutationActionEnum.Upsert,
                    ids.BrgId,
                    ids.DoId,
                    layananId,
                    Quantity: qty,
                    UnitCost: unitCost,
                    ExpirationDate: expiration,
                    Batch: batch,
                    PurchaseOrderId: ids.PoId,
                    LegacyRowId: legacyRowId,
                    SmallestUnitId: "TAB")
            ],
            [
                new LegacyCompatibilityJournalEntryType(
                    LegacyJournalId: legacyJournalId,
                    BrgId: ids.BrgId,
                    ReceiptSourceId: ids.DoId,
                    LayananId: layananId,
                    QuantityIn: qty,
                    QuantityOut: 0m,
                    UnitCost: unitCost,
                    ExpirationDate: expiration,
                    Batch: batch,
                    MutationKindId: "DO",
                    MutationTransactionId: ids.DoId,
                    MutationTime: mutationTime,
                    IsVoid: false,
                    SmallestUnitId: "TAB")
            ]);

        using var trans = TransHelper.NewScope();
        writer.Apply(request);
        trans.Complete();
    }

    private static async Task ReconstructAndSyncAsync(Harness harness, IStockLedgerScopeKey scope)
    {
        var recon = await harness.Reconstruct.Handle(
            new ReconstructStockLedgerBaselineCommand(scope.BrgId, scope.ReceiptSourceId),
            default);
        recon.Outcome.Should().BeOneOf(
            ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed,
            ReconstructStockLedgerBaselineOutcomeEnum.AlreadyReconstructed);

        var sync = await harness.Sync.Handle(
            new SynchronizeStockLedgerScopeCommand(scope.BrgId, scope.ReceiptSourceId),
            default);
        sync.Outcome.Should().BeOneOf(
            SynchronizeStockLedgerScopeOutcomeEnum.Synchronized,
            SynchronizeStockLedgerScopeOutcomeEnum.AlreadyCurrent);
    }

    private static Harness CreateLiveHarness(bool enabled)
    {
        var dbOptions = ConnStringHelper.GetTestEnv();
        var spy = new SpyUnitOfWork();
        var scopeRepo = new StockLedgerScopeStateRepo(new StockLedgerScopeDal(dbOptions));
        var movementRepo = new StockMovementRepo(
            new StockMovementDal(dbOptions),
            new StockMovementLineDal(dbOptions));
        var positionRepo = new StockPositionRepo(
            new StockPositionDal(dbOptions),
            new StockLayerDal(dbOptions));
        var bindingDal = new StockLayerLegacyBindingDal(dbOptions);
        var bindingRepo = new StockLayerLegacyBindingRepo(bindingDal);
        var bindingResolver = new StockLayerLegacyBindingResolver(bindingRepo);
        var idempotencyDal = new StockSourceIdempotencyDal(dbOptions);
        var idempotencyRepo = new StockSourceIdempotencyRepo(idempotencyDal);
        var legacyRead = new LegacyStockReadPort(dbOptions);
        var liveWriter = new LegacyCompatibilityWriterPort(dbOptions);

        var consequenceUow = new StockConsequenceUnitOfWork(
            spy, idempotencyRepo, movementRepo, positionRepo, scopeRepo, liveWriter, bindingRepo);

        var bootstrapper = new LegacySyncIdentityBootstrapper(
            consequenceUow, idempotencyRepo, movementRepo);

        var claim = new ReconstructionClaimService(spy, scopeRepo);
        var reconstruct = new ReconstructStockLedgerBaselineHandler(
            claim, legacyRead, consequenceUow, spy, scopeRepo, idempotencyRepo);

        var discovery = new LegacyChangeDiscoveryPort(legacyRead, idempotencyDal);
        var reconcile = new StockReconciliationPort(
            legacyRead, new StockLayerDal(dbOptions), scopeRepo);
        var snapshotLoader = new LegacySyncLedgerSnapshotLoader(
            idempotencyRepo, positionRepo, movementRepo, bindingRepo);
        var syncClaim = new SynchronizationClaimService(spy, scopeRepo);

        var sync = new SynchronizeStockLedgerScopeHandler(
            scopeRepo,
            discovery,
            legacyRead,
            reconcile,
            consequenceUow,
            spy,
            syncClaim,
            snapshotLoader,
            bootstrapper,
            positionRepo,
            idempotencyRepo);

        var gate = new LegacyStockFreshnessGate(
            scopeRepo,
            discovery,
            legacyRead,
            idempotencyRepo,
            sync.Handle);

        var availability = new AvailabilityDiscoveryPort(dbOptions);
        var orchestrator = new TrustedStockAllocationOrchestrator(
            availability,
            scopeRepo,
            positionRepo,
            gate,
            reconstruct);

        var handler = new PostStockTransferStockConsequenceHandler(
            Options.Create(new StockLedgerStockTransferOptions { Enabled = enabled }),
            orchestrator,
            scopeRepo,
            idempotencyRepo,
            positionRepo,
            consequenceUow,
            legacyRead,
            bootstrapper,
            bindingResolver);

        return new Harness(
            handler,
            reconstruct,
            sync,
            legacyRead,
            new Repos(movementRepo, positionRepo, scopeRepo, idempotencyRepo, bindingRepo));
    }

    private static CaseIds CreateCaseIds(string tag)
    {
        var ulid = Ulid.NewUlid().ToString();
        var shortTag = tag.Length <= 2 ? tag : tag[..2];
        var brgId = $"BRG{shortTag}{ulid}"[..13];
        var doId = $"DO{shortTag}{ulid}"[..10];
        var mtId = $"MT{shortTag}{ulid}"[..10];
        return new CaseIds(
            SourceTxId: $"P5S3-{tag}-{ulid}",
            MtId: mtId,
            BrgId: brgId,
            DoId: doId,
            PoId: $"PO{shortTag}{ulid}"[..10],
            SourceLocationId: $"G{shortTag}{ulid}"[..5],
            DestLocationId: $"H{shortTag}{ulid}"[..5],
            Scope: StockLedgerScopeKeyType.Create(brgId, doId));
    }

    private static (CaseIds IdsA, CaseIds IdsB) CreateMultiItemCaseIds(string tag)
    {
        var idsA = CreateCaseIds($"{tag}A");
        var ulid = Ulid.NewUlid().ToString();
        var shortTag = tag.Length <= 2 ? tag : tag[..2];
        var brgB = $"BRG{shortTag}B{ulid}"[..13];
        var doB = $"DO{shortTag}B{ulid}"[..10];
        var idsB = new CaseIds(
            SourceTxId: $"P5S3-{tag}B-{ulid}",
            MtId: idsA.MtId,
            BrgId: brgB,
            DoId: doB,
            PoId: $"PO{shortTag}B{ulid}"[..10],
            SourceLocationId: idsA.SourceLocationId,
            DestLocationId: idsA.DestLocationId,
            Scope: StockLedgerScopeKeyType.Create(brgB, doB));
        return (idsA, idsB);
    }

    private static void CleanupMultiItem(CaseIds idsA, CaseIds idsB)
    {
        Cleanup(idsA);
        CleanupScope(idsB.BrgId, idsB.DoId, idsB.MtId, idsB.SourceTxId);
    }

    private static (int Stok, int Buku) CountLegacyRows(CaseIds ids)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        var stok = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM tb_stok WHERE fs_kd_barang = @BrgId AND fs_kd_do = @DoId",
            new { ids.BrgId, ids.DoId });
        var buku = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM tb_buku WHERE fs_kd_barang = @BrgId AND fs_kd_do = @DoId",
            new { ids.BrgId, ids.DoId });
        return (stok, buku);
    }

    private static int CountLedgerTransferMovements(CaseIds ids)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        return conn.ExecuteScalar<int>(
            """
            SELECT COUNT(1) FROM BILRG_StokMovement
            WHERE SourceTransactionId = @MtId AND MovementKind = @Kind
            """,
            new { MtId = ids.MtId, Kind = (int)StockMovementKindEnum.Transfer });
    }

    private static void Cleanup(CaseIds ids)
        => CleanupScope(ids.BrgId, ids.DoId, ids.MtId, ids.SourceTxId);

    private static void CleanupScope(string brgId, string doId, string mtId, string sourceTxId)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        conn.Execute(
            """
            DELETE FROM BILRG_StokMovementLine
            WHERE StockMovementId IN (
                SELECT StockMovementId FROM BILRG_StokSourceIdempotency
                WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId AND StockMovementId <> ''
                UNION
                SELECT LayerFormingMovementId FROM BILRG_StokLayer
                WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId
                UNION
                SELECT StockMovementId FROM BILRG_StokMovement
                WHERE SourceTransactionId IN (@MtId, @SourceTxId)
                   OR SourceTransactionId LIKE @SeedLike);
            DELETE FROM BILRG_StokMovement
            WHERE StockMovementId IN (
                SELECT StockMovementId FROM BILRG_StokSourceIdempotency
                WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId AND StockMovementId <> ''
                UNION
                SELECT LayerFormingMovementId FROM BILRG_StokLayer
                WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId
                UNION
                SELECT StockMovementId FROM BILRG_StokMovement
                WHERE SourceTransactionId IN (@MtId, @SourceTxId)
                   OR SourceTransactionId LIKE @SeedLike);
            DELETE FROM BILRG_StokLayerLegacyBinding WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId;
            DELETE FROM BILRG_StokLayer WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId;
            DELETE FROM BILRG_StokPosition WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId;
            DELETE FROM BILRG_StokLedgerScope WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId;
            DELETE FROM BILRG_StokSourceIdempotency
            WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId;
            DELETE FROM BILRG_StokSourceIdempotency
            WHERE IdempotencyKey = @MtKey
               OR IdempotencyKey LIKE @MtKeyFpLike;
            DELETE FROM tb_stok WHERE fs_kd_barang = @BrgId AND fs_kd_do = @DoId;
            DELETE FROM tb_buku WHERE fs_kd_barang = @BrgId AND fs_kd_do = @DoId;
            """,
            new
            {
                BrgId = brgId,
                DoId = doId,
                MtId = mtId,
                SourceTxId = sourceTxId,
                SeedLike = $"SEED-{sourceTxId}%",
                MtKey = TransferConsequenceIdempotency.BuildSourceConsequenceKey(mtId),
                MtKeyFpLike = $"{TransferConsequenceIdempotency.BuildSourceConsequenceKey(mtId)}|FP|%"
            });
    }

    private sealed record CaseIds(
        string SourceTxId,
        string MtId,
        string BrgId,
        string DoId,
        string PoId,
        string SourceLocationId,
        string DestLocationId,
        IStockLedgerScopeKey Scope);

    private sealed record Repos(
        IStockMovementRepo Movement,
        IStockPositionRepo Position,
        IStockLedgerScopeStateRepo Scope,
        IStockSourceIdempotencyRepo Idempotency,
        IStockLayerLegacyBindingRepo Binding);

    private sealed record Harness(
        PostStockTransferStockConsequenceHandler Handler,
        ReconstructStockLedgerBaselineHandler Reconstruct,
        SynchronizeStockLedgerScopeHandler Sync,
        LegacyStockReadPort LegacyRead,
        Repos Repos);

    #endregion
}
