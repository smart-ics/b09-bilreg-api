using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class StockBatchDomainTest
{
    private static readonly DateTime TglMasuk = new(2026, 3, 1, 10, 0, 0);
    private static readonly DateTime TglEd = new(2027, 6, 30);
    private const string BrgId = "BRG0000000001";
    private const string DoId = "DO00000001";
    private const string LayananA = "LY001";
    private const string LayananB = "LY002";

    [Fact]
    public void Create_InitializesBatchWithZeroQty()
    {
        var batch = StockBatchModel.Create(BrgId, DoId, hpp: 1500.50m, TglMasuk, poReffId: "PO00000001");

        batch.StokBatchId.Should().NotBeNullOrWhiteSpace();
        batch.BrgId.Should().Be(BrgId);
        batch.BrgMasukReffId.Should().Be(DoId);
        batch.Hpp.Should().Be(1500.50m);
        batch.TglMasuk.Should().Be(TglMasuk);
        batch.PoReffId.Should().Be("PO00000001");
        batch.QtySisa.Should().Be(0);
        batch.Version.Should().Be(0);
        batch.ListLokasi.Should().BeEmpty();
        batch.LokasiQtyTotal.Should().Be(0);
    }

    [Fact]
    public void IncreaseLokasi_IncreasesBatchAndLokasiQty()
    {
        var batch = StockBatchModel.Create(BrgId, DoId, hpp: 100m, TglMasuk);

        var lokasi = batch.IncreaseLokasi(LayananA, TglEd, qty: 10);

        lokasi.QtySisa.Should().Be(10);
        lokasi.StokBatchId.Should().Be(batch.StokBatchId);
        lokasi.BrgId.Should().Be(BrgId);
        lokasi.BrgMasukReffId.Should().Be(DoId);
        lokasi.TglMasuk.Should().Be(TglMasuk);
        batch.QtySisa.Should().Be(10);
        batch.LokasiQtyTotal.Should().Be(10);
        batch.ListLokasi.Should().ContainSingle();
    }

    [Fact]
    public void IncreaseLokasi_MultipleLocations_SumsHospitalWideQty()
    {
        var batch = StockBatchModel.Create(BrgId, DoId, hpp: 100m, TglMasuk);

        batch.IncreaseLokasi(LayananA, TglEd, qty: 7);
        batch.IncreaseLokasi(LayananB, TglEd, qty: 3);

        batch.ListLokasi.Should().HaveCount(2);
        batch.QtySisa.Should().Be(10);
        batch.LokasiQtyTotal.Should().Be(10);
    }

    [Fact]
    public void DecreaseLokasi_ReducesQty()
    {
        var batch = StockBatchModel.Create(BrgId, DoId, hpp: 100m, TglMasuk);
        batch.IncreaseLokasi(LayananA, TglEd, qty: 10);

        batch.DecreaseLokasi(LayananA, TglEd, qty: 4);

        batch.ListLokasi.Single().QtySisa.Should().Be(6);
        batch.QtySisa.Should().Be(6);
        batch.LokasiQtyTotal.Should().Be(6);
    }

    [Fact]
    public void DecreaseLokasi_RejectNegative()
    {
        var batch = StockBatchModel.Create(BrgId, DoId, hpp: 100m, TglMasuk);
        batch.IncreaseLokasi(LayananA, TglEd, qty: 5);

        var act = () => batch.DecreaseLokasi(LayananA, TglEd, qty: 6);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BR-STL-010*");
        batch.ListLokasi.Single().QtySisa.Should().Be(5);
        batch.QtySisa.Should().Be(5);
    }

    [Fact]
    public void DecreaseLokasi_ToZero_RetainsDepletedBalance()
    {
        var batch = StockBatchModel.Create(BrgId, DoId, hpp: 100m, TglMasuk);
        batch.IncreaseLokasi(LayananA, TglEd, qty: 5);

        batch.DecreaseLokasi(LayananA, TglEd, qty: 5);

        batch.ListLokasi.Should().ContainSingle();
        batch.ListLokasi.Single().QtySisa.Should().Be(0);
        batch.QtySisa.Should().Be(0);
        batch.LokasiQtyTotal.Should().Be(0);
    }

    [Fact]
    public void IncreaseLokasi_SameLayananAndEd_AccumulatesOnSameBalance()
    {
        var batch = StockBatchModel.Create(BrgId, DoId, hpp: 100m, TglMasuk);

        batch.IncreaseLokasi(LayananA, TglEd, qty: 4);
        batch.IncreaseLokasi(LayananA, TglEd, qty: 6);

        batch.ListLokasi.Should().ContainSingle();
        batch.ListLokasi.Single().QtySisa.Should().Be(10);
        batch.QtySisa.Should().Be(10);
    }
}
