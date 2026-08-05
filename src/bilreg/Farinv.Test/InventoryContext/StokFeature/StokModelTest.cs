using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Domain.InventoryContext.StokFeature;

namespace Farinv.Test.InventoryContext.StokFeature;

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

public class StokModelTests
{
    private static StokModel CreateEmptyStok()
    {
        return new StokModel(
            brgId: "BRG001",
            layananId: "LAY001",
            brg: BrgObatType.Default.ToReff(),
            layanan: LayananType.Default.ToReff(),
            qty: 0,
            satuan: "PCS",
            listLayer: []
        );
    }

    private static StokLotType CreateLot(DateOnly expDate)
        => new StokLotType("PO-1", "RCV-1", expDate, "BATCH");

    [Fact]
    public void UT01_GivenEmptyStock_WhenAddStock_ThenNewLayerCreatedAndQtyUpdated()
    {
        // Arrange
        var stok = CreateEmptyStok();
        var lot = CreateLot(new DateOnly(2025, 12, 31));

        // Act
        stok.AddStok(100, lot, 1000, new TrsReffType("PO-001", DateTime.Today), "PURCHASE");

        // Assert
        stok.Qty.Should().Be(100);
        stok.ListLayer.Should().HaveCount(1);

        var layer = stok.ListLayer.Single();
        layer.QtyIn.Should().Be(100);
        layer.QtySisa.Should().Be(100);
        layer.ListBuku.Should().HaveCount(1);
    }

    [Fact]
    public void UT02_GivenStockWithSingleLayer_WhenRemovePartialQty_ThenQtySisaReduced()
    {
        // Arrange
        var stok = CreateEmptyStok();
        stok.AddStok(100, CreateLot(new DateOnly(2025, 12, 31)), 1000,
            new TrsReffType("PO-001", DateTime.Today), "PURCHASE");

        // Act
        stok.RemoveStok(30,new TrsReffType("USE-001", DateTime.Today), "PAKAI");

        // Assert
        stok.Qty.Should().Be(70);

        var layer = stok.ListLayer.Single();
        layer.QtySisa.Should().Be(70);
        layer.ListBuku.Should().HaveCount(2);
    }

    [Fact]
    public void UT03_GivenStock_WhenRemoveQtyExceedTotal_ThenThrowException()
    {
        // Arrange
        var stok = CreateEmptyStok();
        stok.AddStok(50, CreateLot(new DateOnly(2025, 12, 31)), 1000,
            new TrsReffType("PO-001", DateTime.Today), "PURCHASE");

        // Act
        Action act = () =>
            stok.RemoveStok(60, new TrsReffType("USE-001", DateTime.Today), "PAKAI");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Qty tidak boleh melebihi Qty Stok*");
    }

    [Fact]
    public void UT04_GivenMultipleLayersWithDifferentExpDate_WhenRemoveStock_ThenUseFEFO()
    {
        // Arrange
        var stok = CreateEmptyStok();

        stok.AddStok(50, CreateLot(new DateOnly(2024, 12, 31)), 1000,
            new TrsReffType("PO-OLD", DateTime.Today), "PURCHASE");

        stok.AddStok(50, CreateLot(new DateOnly(2025, 12, 31)), 1100,
            new TrsReffType("PO-NEW", DateTime.Today), "PURCHASE");

        // Act
        stok.RemoveStok(30, new TrsReffType("USE-001", DateTime.Today), "PAKAI");

        // Assert
        stok.Qty.Should().Be(70);

        var firstLayer = stok.ListLayer
            .OrderBy(x => x.StokLot.ExpDate)
            .First();

        firstLayer.QtySisa.Should().Be(20);
    }

    [Fact]
    public void UT05_GivenRemoveQtyCrossingLayers_WhenRemoveStock_ThenConsumeMultipleLayers()
    {
        // Arrange
        var stok = CreateEmptyStok();

        stok.AddStok(40, CreateLot(new DateOnly(2024, 12, 31)), 1000,
            new TrsReffType("PO-1", DateTime.Today), "PURCHASE");

        stok.AddStok(60, CreateLot(new DateOnly(2025, 12, 31)), 1100,
            new TrsReffType("PO-2", DateTime.Today), "PURCHASE");

        // Act
        stok.RemoveStok(50, new TrsReffType("USE-001", DateTime.Today), "PAKAI");

        // Assert
        stok.Qty.Should().Be(50);

        var layers = stok.ListLayer
            .OrderBy(x => x.StokLot.ExpDate)
            .ToList();

        layers[0].QtySisa.Should().Be(0);
        layers[1].QtySisa.Should().Be(50);
    }

    [Fact]
    public void UT06_GivenReturnStock_WhenAddStock_ThenCreateNewLayer()
    {
        // Arrange
        var stok = CreateEmptyStok();
        var stokLot = CreateLot(DateOnly.FromDateTime(new DateTime(2025, 12, 15)));
        stok.AddStok(100, stokLot, 1000,
            new TrsReffType("PO-001", DateTime.Today), "PURCHASE");

        // Act
        stok.AddStok(15, CreateLot(new DateOnly(2025, 12, 31)), 1000,
            new TrsReffType("RET-001", DateTime.Today), "RETURN");

        // Assert
        stok.ListLayer.Should().HaveCount(2);
        stok.Qty.Should().Be(115);
    }

    [Fact]
    public void UT07_GivenLayer_WhenRemoveStock_ThenMovementRecordedCorrectly()
    {
        // Arrange
        var stok = CreateEmptyStok();
        stok.AddStok(20, CreateLot(new DateOnly(2025, 12, 31)), 1000,
            new TrsReffType("PO-001", DateTime.Today), "PURCHASE");

        var layer = stok.ListLayer.Single();

        // Act
        stok.RemoveStok(5, new TrsReffType("USE-001", DateTime.Today), "PAKAI");

        // Assert
        var movements = layer.ListBuku.ToList();

        movements.Should().HaveCount(2);
        movements.Sum(x => x.QtyIn - x.QtyOut).Should().Be(15);
    }
}
