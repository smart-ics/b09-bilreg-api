using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class StockMovementDomainTest
{
    private static readonly DateTime TglEd = new(2027, 6, 30);
    private static readonly DateTime TglMutasi = new(2026, 4, 1, 14, 30, 0);

    [Fact]
    public void CreateInbound_SetsQtyInOnly()
    {
        var mutasi = StockMovementModel.CreateInbound(
            stokLokasiId: "SL0000000001",
            stokBatchId: "SB0000000001",
            brgId: "BRG0000000001",
            brgMasukReffId: "DO00000001",
            layananId: "LY001",
            tglEd: TglEd,
            trsReffId: "TR00000001",
            movementKind: MovementKindEnum.GoodsReceipt,
            qtyIn: 12,
            hpp: 100.25m,
            tglMutasi: TglMutasi);

        mutasi.StokMutasiId.Should().NotBeNullOrWhiteSpace();
        mutasi.QtyIn.Should().Be(12);
        mutasi.QtyOut.Should().Be(0);
        mutasi.MovementKind.Should().Be(MovementKindEnum.GoodsReceipt);
        mutasi.Hpp.Should().Be(100.25m);
    }

    [Fact]
    public void CreateOutbound_SetsQtyOutOnly()
    {
        var mutasi = StockMovementModel.CreateOutbound(
            stokLokasiId: "SL0000000001",
            stokBatchId: "SB0000000001",
            brgId: "BRG0000000001",
            brgMasukReffId: "DO00000001",
            layananId: "LY001",
            tglEd: TglEd,
            trsReffId: "TR00000002",
            movementKind: MovementKindEnum.SaleIssueDb,
            qtyOut: 3,
            hpp: 100.25m,
            tglMutasi: TglMutasi);

        mutasi.QtyIn.Should().Be(0);
        mutasi.QtyOut.Should().Be(3);
        mutasi.MovementKind.Should().Be(MovementKindEnum.SaleIssueDb);
    }

    [Fact]
    public void Create_RejectsBothSidesPositive()
    {
        var act = () => new StockMovementModel(
            "SM0000000001", "SL0000000001", "SB0000000001", "BRG0000000001", "DO00000001",
            "LY001", TglEd, "TR00000003", MovementKindEnum.GoodsReceipt,
            qtyIn: 5, qtyOut: 2, hpp: 10, poReffId: "", tglMutasi: TglMutasi, reversesMutasiId: "");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*QtyIn*QtyOut*");
    }

    [Fact]
    public void Create_RejectsBothSidesZero()
    {
        var act = () => new StockMovementModel(
            "SM0000000001", "SL0000000001", "SB0000000001", "BRG0000000001", "DO00000001",
            "LY001", TglEd, "TR00000003", MovementKindEnum.GoodsReceipt,
            qtyIn: 0, qtyOut: 0, hpp: 10, poReffId: "", tglMutasi: TglMutasi, reversesMutasiId: "");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*QtyIn*QtyOut*");
    }

    [Fact]
    public void Create_DefaultsReversesMutasiIdEmpty()
    {
        var inbound = StockMovementModel.CreateInbound(
            "SL0000000001", "SB0000000001", "BRG0000000001", "DO00000001", "LY001",
            TglEd, "TR00000004", MovementKindEnum.GoodsReceipt, 1, 10m, TglMutasi);

        var outbound = StockMovementModel.CreateOutbound(
            "SL0000000001", "SB0000000001", "BRG0000000001", "DO00000001", "LY001",
            TglEd, "TR00000005", MovementKindEnum.SaleIssueDu, 1, 10m, TglMutasi);

        inbound.ReversesMutasiId.Should().Be(string.Empty);
        outbound.ReversesMutasiId.Should().Be(string.Empty);
    }
}
