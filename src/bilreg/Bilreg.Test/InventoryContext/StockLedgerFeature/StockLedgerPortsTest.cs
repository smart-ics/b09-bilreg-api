using System.Reflection;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using Bilreg.Test.InventoryContext.StockLedgerFeature.Fakes;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P1-S7 — Application port contracts and Test doubles only.
/// No live legacy adapters, discovery behavior, or FO writes.
/// </summary>
public class StockLedgerPortsTest
{
    [Fact]
    public void UT01_AllRequiredPorts_AreDefinedInApplicationPortsNamespace()
    {
        var application = typeof(ILegacyStockReadPort).Assembly;
        var portNames = new[]
        {
            "Bilreg.Application.InventoryContext.StockLedgerFeature.Ports.ILegacyStockReadPort",
            "Bilreg.Application.InventoryContext.StockLedgerFeature.Ports.ILegacyCompatibilityWriterPort",
            "Bilreg.Application.InventoryContext.StockLedgerFeature.Ports.ILegacyChangeDiscoveryPort",
            "Bilreg.Application.InventoryContext.StockLedgerFeature.Ports.IAvailabilityDiscoveryPort",
            "Bilreg.Application.InventoryContext.StockLedgerFeature.Ports.IProvenanceDiscoveryPort",
            "Bilreg.Application.InventoryContext.StockLedgerFeature.Ports.IStockReconciliationPort"
        };

        foreach (var name in portNames)
        {
            var type = application.GetType(name, throwOnError: false);
            type.Should().NotBeNull($"port {name} must exist");
            type!.IsInterface.Should().BeTrue();
        }
    }

    [Fact]
    public void UT02_TestFakes_ImplementAllPorts()
    {
        typeof(ILegacyStockReadPort).IsAssignableFrom(typeof(FakeLegacyStockReadPort)).Should().BeTrue();
        typeof(ILegacyCompatibilityWriterPort).IsAssignableFrom(typeof(FakeLegacyCompatibilityWriterPort)).Should().BeTrue();
        typeof(ILegacyChangeDiscoveryPort).IsAssignableFrom(typeof(FakeLegacyChangeDiscoveryPort)).Should().BeTrue();
        typeof(IAvailabilityDiscoveryPort).IsAssignableFrom(typeof(FakeAvailabilityDiscoveryPort)).Should().BeTrue();
        typeof(IProvenanceDiscoveryPort).IsAssignableFrom(typeof(FakeProvenanceDiscoveryPort)).Should().BeTrue();
        typeof(IStockReconciliationPort).IsAssignableFrom(typeof(FakeStockReconciliationPort)).Should().BeTrue();
    }

    [Fact]
    public void UT03_FakeLegacyStockReadPort_ReturnsConfiguredEmptySnapshots()
    {
        var sut = new FakeLegacyStockReadPort();
        var scope = StockLedgerScopeKeyType.Create("BRG01", "DO001");

        sut.ListCurrentBalances(scope).Should().BeEmpty();
        sut.ListJournalEntries(scope).Should().BeEmpty();
        sut.BalanceRequests.Should().ContainSingle().Which.Should().Be(scope);
        sut.JournalRequests.Should().ContainSingle().Which.Should().Be(scope);
    }

    [Fact]
    public void UT04_FakeLegacyCompatibilityWriter_RecordsApply_AndCanThrowForRollbackHarness()
    {
        var happy = new FakeLegacyCompatibilityWriterPort();
        var request = new LegacyCompatibilityWriteRequest(
            SourceTransactionReferenceType.Create("FO-1"),
            StockMovementKindEnum.Receipt,
            StockLedgerScopeKeyType.Create("BRG01", "DO001"),
            Array.Empty<LegacyCompatibilityBalanceMutationType>(),
            Array.Empty<LegacyCompatibilityJournalEntryType>());

        happy.Apply(request);
        happy.Applied.Should().ContainSingle().Which.Should().Be(request);

        var failing = new FakeLegacyCompatibilityWriterPort { ThrowOnApply = true };
        var act = () => failing.Apply(request);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Fake legacy compatibility writer failure*");
        failing.Applied.Should().BeEmpty();
    }

    [Fact]
    public void UT05_FakeLegacyChangeDiscovery_DefaultIsUnchanged_WithOpaqueFingerprint()
    {
        var sut = new FakeLegacyChangeDiscoveryPort();
        var scope = StockLedgerScopeKeyType.Create("BRG01", "DO001");

        var fingerprint = sut.ComputeCurrentFingerprint(scope);
        fingerprint.AlgorithmVersion.Should().Be("test-v1");
        fingerprint.OpaqueValue.Should().NotBeEmpty();

        var result = sut.DiscoverChanges(scope, storedPosition: null);
        result.Outcome.Should().Be(LegacyChangeDiscoveryOutcomeEnum.Unchanged);
        result.Deltas.Should().BeEmpty();
        result.CurrentFingerprint.Should().NotBeNull();
    }

    [Fact]
    public void UT06_FakeAvailabilityDiscovery_DoesNotInventCandidatesByDefault()
    {
        var sut = new FakeAvailabilityDiscoveryPort();
        var item = BrgObatType.Default.ToReff() with { BrgId = "BRG01" };
        var location = LayananType.Key("LY01");

        var result = sut.Discover(item, location, expirationDateFilter: null);

        result.Outcome.Should().Be(AvailabilityDiscoveryOutcomeEnum.InsufficientAuthoritativeStock);
        result.Candidates.Should().BeEmpty();
    }

    [Fact]
    public void UT07_FakeProvenanceDiscovery_DefaultsToUnknown_DoesNotInventReceiptSource()
    {
        var sut = new FakeProvenanceDiscoveryPort();
        var request = new ProvenanceDiscoveryRequest(
            SourceTransactionReferenceType.Create("FO-RET-1"),
            BrgObatType.Default.ToReff() with { BrgId = "BRG01" },
            LayananType.Key("LY01"));

        var result = sut.Discover(request);

        result.Outcome.Should().Be(ProvenanceDiscoveryOutcomeEnum.Unknown);
        result.ReceiptSourceId.Should().BeNull();
        result.StockLayerId.Should().BeNull();
        result.CandidateReceiptSourceIds.Should().BeEmpty();
    }

    [Fact]
    public void UT08_FakeStockReconciliation_DefaultsToBalancedWithoutAuthorityClaim()
    {
        var sut = new FakeStockReconciliationPort();
        var scope = StockLedgerScopeKeyType.Create("BRG01", "DO001");

        var result = sut.Reconcile(scope);

        result.Outcome.Should().Be(StockReconciliationOutcomeEnum.Balanced);
        result.Differences.Should().BeEmpty();
    }

    [Fact]
    public void UT09_PortContractModels_HaveNoIsAuthoritativeMembers()
    {
        var assembly = typeof(ILegacyStockReadPort).Assembly;
        var portTypes = assembly
            .GetTypes()
            .Where(t => t.Namespace == "Bilreg.Application.InventoryContext.StockLedgerFeature.Ports");

        foreach (var type in portTypes)
        {
            var names = type
                .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Select(m => m.Name);
            names.Should().NotContain("IsAuthoritative", because: $"{type.Name} must not encode authority");
        }
    }

    [Fact]
    public void UT10_PortFakes_LiveInTestAssembly_NotApplication()
    {
        typeof(FakeLegacyCompatibilityWriterPort).Assembly
            .GetName().Name.Should().Be("Bilreg.Test");

        var application = typeof(ILegacyCompatibilityWriterPort).Assembly;
        application.GetTypes()
            .Where(t => t.Name.StartsWith("Fake", StringComparison.Ordinal))
            .Should().BeEmpty("production Application must not host Stock Ledger port fakes");
    }
}
