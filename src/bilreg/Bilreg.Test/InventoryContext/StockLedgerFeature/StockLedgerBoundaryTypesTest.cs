using System.Reflection;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class StockLedgerBoundaryTypesTest
{
    [Fact]
    public void UT01_ReceiptSource_SameId_AreEqual()
    {
        var a = ReceiptSourceType.Create("DO001");
        var b = ReceiptSourceType.Create("DO001");

        a.Should().Be(b);
        a.ReceiptSourceId.Should().Be("DO001");
    }

    [Fact]
    public void UT02_ReceiptSource_DifferentId_AreNotEqual()
    {
        var a = ReceiptSourceType.Create("DO001");
        var b = ReceiptSourceType.Create("DO002");

        a.Should().NotBe(b);
    }

    [Fact]
    public void UT03_LedgerScope_EqualsByItemAndReceiptSource_IgnoresLocation()
    {
        var scopeA = StockLedgerScopeKeyType.Create("BRG01", "DO001");
        var scopeB = StockLedgerScopeKeyType.Create(
            BrgObatType.Default.ToReff() with { BrgId = "BRG01" },
            ReceiptSourceType.Create("DO001"));

        scopeA.Should().Be(scopeB);
        scopeA.Should().BeEquivalentTo(StockLedgerScopeKeyType.Key("BRG01", "DO001"));
    }

    [Fact]
    public void UT04_LedgerScope_DifferentReceiptSource_AreNotEqual()
    {
        var a = StockLedgerScopeKeyType.Create("BRG01", "DO001");
        var b = StockLedgerScopeKeyType.Create("BRG01", "DO002");

        a.Should().NotBe(b);
    }

    [Fact]
    public void UT05_WriteScope_IncludesLocation_DistinctFromLedgerScope()
    {
        var writeApotek = StockWriteScopeKeyType.Create("BRG01", "DO001", "APT01");
        var writeGudang = StockWriteScopeKeyType.Create("BRG01", "DO001", "GDN01");
        var ledger = StockLedgerScopeKeyType.Create("BRG01", "DO001");

        writeApotek.Should().NotBe(writeGudang);
        writeApotek.ToLedgerScope().Should().Be(ledger);
        writeGudang.ToLedgerScope().Should().Be(ledger);
        writeApotek.ToLedgerScope().Should().Be(writeGudang.ToLedgerScope());
    }

    [Fact]
    public void UT06_WriteScope_SameItemReceiptSourceLocation_AreEqual()
    {
        var a = StockWriteScopeKeyType.Create(
            new BrgReff("BRG01", "Item"),
            ReceiptSourceType.Create("DO001"),
            LayananType.Key("APT01"));
        var b = StockWriteScopeKeyType.Create("BRG01", "DO001", "APT01");

        a.Should().Be(b);
        a.LayananId.Should().Be("APT01");
    }

    [Fact]
    public void UT07_WriteScope_IsLedgerScopePlusLocation()
    {
        IStockWriteScopeKey write = StockWriteScopeKeyType.Create("BRG01", "DO001", "APT01");
        IStockLedgerScopeKey ledger = write;

        ledger.BrgId.Should().Be("BRG01");
        ledger.ReceiptSourceId.Should().Be("DO001");
        write.LayananId.Should().Be("APT01");
    }

    [Fact]
    public void UT08_StockFactOrigin_MatchesApprovedDomainSet()
    {
        Enum.GetNames<StockFactOriginEnum>().Should().BeEquivalentTo(
            "Native", "Reconstructed", "LegacySynchronized");
    }

    [Fact]
    public void UT09_ReconstructionStatus_MatchesApprovedDomainSet()
    {
        Enum.GetNames<ReconstructionStatusEnum>().Should().BeEquivalentTo(
            "NotReconstructed",
            "ReconstructionRequired",
            "Reconstructing",
            "Reconstructed",
            "Inconsistent");
    }

    [Fact]
    public void UT10_SynchronizationState_MatchesApprovedDomainSet()
    {
        Enum.GetNames<SynchronizationStateEnum>().Should().BeEquivalentTo(
            "Current",
            "LegacyChangePending",
            "SynchronizationRequired",
            "Inconsistent");
    }

    [Fact]
    public void UT11_BoundaryTypes_HaveNoAuthoritySemantics()
    {
        var types = new[]
        {
            typeof(ReceiptSourceType),
            typeof(StockLedgerScopeKeyType),
            typeof(StockWriteScopeKeyType),
            typeof(StockFactOriginEnum),
            typeof(ReconstructionStatusEnum),
            typeof(SynchronizationStateEnum)
        };

        foreach (var type in types)
        {
            type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Select(m => m.Name)
                .Should()
                .NotContain(name => name.Contains("Authoritative", StringComparison.OrdinalIgnoreCase),
                    because: $"{type.Name} must not encode authority");
        }

        Enum.GetNames<StockFactOriginEnum>()
            .Should()
            .NotContain(name => name.Contains("Authoritative", StringComparison.OrdinalIgnoreCase));
    }
}
