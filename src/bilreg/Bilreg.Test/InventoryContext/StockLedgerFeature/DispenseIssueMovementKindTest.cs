using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class DispenseIssueMovementKindTest
{
    [Fact]
    public void DispenseIssue_IsDistinctFromSaleIssueDu()
    {
        MovementKindEnum.DispenseIssue.Should().NotBe(MovementKindEnum.SaleIssueDu);
        MovementKindEnum.DispenseIssue.Should().NotBe(MovementKindEnum.SaleIssueDb);
        MovementKindEnum.DispenseIssue.Should().NotBe(MovementKindEnum.SaleIssueDt);
        ((int)MovementKindEnum.DispenseIssue).Should().Be(14);
    }

    [Fact]
    public void CreateOutbound_AcceptsDispenseIssue()
    {
        var mutasi = StockMovementModel.CreateOutbound(
            "SL0000000001",
            "SB0000000001",
            "BRG0000000001",
            "DO00000001",
            "LYDTU",
            new DateTime(2027, 6, 30),
            "ADP000000001:I1:DISPENSE_ISSUE",
            MovementKindEnum.DispenseIssue,
            qtyOut: 2,
            hpp: 10m,
            tglMutasi: new DateTime(2026, 8, 18));

        mutasi.MovementKind.Should().Be(MovementKindEnum.DispenseIssue);
        mutasi.QtyOut.Should().Be(2);
        mutasi.QtyIn.Should().Be(0);
        mutasi.LayananId.Should().Be("LYDTU");
    }

    [Fact]
    public void HandlerType_ExistsForDispenseIssueConsequence()
    {
        typeof(PostDispenseIssueConsequenceHandler).Should().NotBeNull();
        typeof(PostDispenseIssueConsequenceCommand).Should().NotBeNull();
    }
}
