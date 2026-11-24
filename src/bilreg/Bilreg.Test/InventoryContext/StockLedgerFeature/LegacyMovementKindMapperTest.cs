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
    [InlineData("PK_V", MovementKindEnum.InternalConsumptionVoid)]
    [InlineData("pk_v", MovementKindEnum.InternalConsumptionVoid)]
    [InlineData("AJ_PLUS", MovementKindEnum.AdjustmentPlus)]
    [InlineData("aj_plus", MovementKindEnum.AdjustmentPlus)]
    [InlineData("AJ_PLUS_V", MovementKindEnum.AdjustmentPlusVoid)]
    [InlineData("AJ_MIN", MovementKindEnum.AdjustmentMinus)]
    [InlineData("AJ_MIN_V", MovementKindEnum.AdjustmentMinusVoid)]
    [InlineData("DO_V", MovementKindEnum.GoodsReceiptVoid)]
    [InlineData("MT_OUT_V", MovementKindEnum.TransferOutVoid)]
    [InlineData("MT_IN_V", MovementKindEnum.TransferInVoid)]
    [InlineData("RJ_V", MovementKindEnum.SalesReturnRjVoid)]
    [InlineData("RU_V", MovementKindEnum.SalesReturnRuVoid)]
    [InlineData("RT_V", MovementKindEnum.SalesReturnRtVoid)]
    [InlineData("RP_ASAL", MovementKindEnum.RepackSource)]
    [InlineData("RP_ASAL_V", MovementKindEnum.RepackSourceVoid)]
    [InlineData("RP_HASIL", MovementKindEnum.RepackOutput)]
    [InlineData("RP_HASIL_V", MovementKindEnum.RepackOutputVoid)]
    [InlineData("MN", MovementKindEnum.DisposalIssue)]
    [InlineData("MN_V", MovementKindEnum.DisposalIssueVoid)]
    [InlineData("RB", MovementKindEnum.PurchaseReturn)]
    [InlineData("RB_V", MovementKindEnum.PurchaseReturnVoid)]
    [InlineData("DU-S", MovementKindEnum.SatelliteIssue)]
    [InlineData("DU-S_V", MovementKindEnum.SatelliteIssueVoid)]
    [InlineData("DU-R_IN", MovementKindEnum.SatelliteReserveIn)]
    [InlineData("DU-R_IN_V", MovementKindEnum.SatelliteReserveInVoid)]
    [InlineData("DU-R_OUT", MovementKindEnum.SatelliteReserveOut)]
    [InlineData("DU-R_OUT_V", MovementKindEnum.SatelliteReserveOutVoid)]
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
    [InlineData("UNKNOWN_CODE")]
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

    [Fact]
    public void TryMap_Reverse_InternalConsumptionVoid_ReturnsPkV()
    {
        LegacyMovementKindMapper.TryMap(MovementKindEnum.InternalConsumptionVoid, out var legacy).Should().BeTrue();
        legacy.Should().Be("PK_V");

        LegacyMovementKindMapper.ToLegacyString(MovementKindEnum.InternalConsumptionVoid).Should().Be("PK_V");
    }
}
