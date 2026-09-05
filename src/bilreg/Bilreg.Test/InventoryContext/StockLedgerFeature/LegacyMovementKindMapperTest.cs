using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class LegacyMovementKindMapperTest
{
    [Theory]
    [InlineData("DO", MovementKindEnum.GoodsReceipt)]
    [InlineData("MT_OUT", MovementKindEnum.TransferOut)]
    [InlineData("MT_IN", MovementKindEnum.TransferIn)]
    [InlineData("DB", MovementKindEnum.SaleIssueDb)]
    [InlineData("DU", MovementKindEnum.SaleIssueDu)]
    [InlineData("DT", MovementKindEnum.SaleIssueDt)]
    [InlineData("PK", MovementKindEnum.InternalConsumption)]
    [InlineData("RJ", MovementKindEnum.SalesReturnRj)]
    [InlineData("RU", MovementKindEnum.SalesReturnRu)]
    [InlineData("RT", MovementKindEnum.SalesReturnRt)]
    [InlineData("DB_V", MovementKindEnum.SaleVoidDb)]
    [InlineData("DU_V", MovementKindEnum.SaleVoidDu)]
    [InlineData("DT_V", MovementKindEnum.SaleVoidDt)]
    [InlineData("DI", MovementKindEnum.DispenseIssue)]
    [InlineData("di", MovementKindEnum.DispenseIssue)]
    [InlineData("do", MovementKindEnum.GoodsReceipt)]
    public void TryMap_S1Strings_Maps(string legacy, MovementKindEnum expected)
    {
        LegacyMovementKindMapper.TryMap(legacy, out var kind).Should().BeTrue();
        kind.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("XX")]
    [InlineData("DO_V")]
    [InlineData("MT_OUT_V")]
    public void TryMap_UnknownOrEmpty_Fails(string? legacy)
    {
        LegacyMovementKindMapper.TryMap(legacy, out _).Should().BeFalse();
    }

    [Fact]
    public void TryMap_Di_IsDistinctFromDu()
    {
        LegacyMovementKindMapper.TryMap("DI", out var dispense).Should().BeTrue();
        LegacyMovementKindMapper.TryMap("DU", out var sale).Should().BeTrue();
        dispense.Should().Be(MovementKindEnum.DispenseIssue);
        sale.Should().Be(MovementKindEnum.SaleIssueDu);
        dispense.Should().NotBe(sale);
    }
}
