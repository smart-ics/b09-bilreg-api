using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public class OrderMutasiModelTests
{
    private static LayananType LayananOrder() => LayananType.Default with { LayananId = "L01", LayananName = "Poli Penyakit Dalam" };
    private static LayananType LayananTujuan() => LayananType.Default with { LayananId = "L02", LayananName = "Gudang Farmasi Pusat" };
    private static BrgReff SampleBrg(string id = "B01", string name = "Paracetamol 500mg") => new(id, name);
    private static SatuanType SampleSatuan() => new("TAB", "Tablet");

    private static OrderMutasiModel CreateDraftOrder(LayananType? order = null, LayananType? tujuan = null)
    {
        return OrderMutasiModel.Create(
            order ?? LayananOrder(),
            tujuan ?? LayananTujuan(),
            orderNote: "Permintaan rutin mingguan",
            userId: "user-admin",
            listItem: [],
            priority: OrderMutasiPriorityEnum.Rutin);
    }

    private static OrderMutasiItemModel CreateItem(string brgId = "B01", decimal qty = 10, decimal stokCurrent = 5)
    {
        return new OrderMutasiItemModel(
            noUrut: 1,
            brg: SampleBrg(brgId),
            qty: qty,
            satuan: SampleSatuan(),
            stokCurrent: stokCurrent,
            qtyTerpenuhi: 0);
    }

    [Fact]
    public void GivenNewDraft_WhenAddItem_ThenItemAddedWithStokCurrentAndSisaBackorderCalculated()
    {
        // Arrange
        var order = CreateDraftOrder();
        var item = CreateItem("B01", qty: 10, stokCurrent: 5);

        // Act
        order.AddItem(item);

        // Assert
        order.ListItem.Should().HaveCount(1);
        var added = order.ListItem.First();
        added.Brg.BrgId.Should().Be("B01");
        added.Qty.Should().Be(10);
        added.StokCurrent.Should().Be(5);
        added.QtyTerpenuhi.Should().Be(0);
        added.SisaBackorder.Should().Be(10);
        order.State.Should().Be(OrderMutasiStateEnum.Draft);
    }

    [Fact]
    public void GivenDraftWithExistingItem_WhenAddSameItem_ThenAccumulatesQtyAndUpdatesStokCurrent()
    {
        // Arrange
        var order = CreateDraftOrder();
        order.AddItem(CreateItem("B01", qty: 10, stokCurrent: 5));
        var duplicate = CreateItem("B01", qty: 15, stokCurrent: 8);

        // Act
        order.AddItem(duplicate);

        // Assert
        order.ListItem.Should().HaveCount(1);
        var item = order.ListItem.First();
        item.Qty.Should().Be(25);
        item.StokCurrent.Should().Be(8);
        item.SisaBackorder.Should().Be(25);
    }

    [Fact]
    public void GivenNonPositiveQty_WhenAddItem_ThenThrowsArgumentException()
    {
        // Arrange
        var order = CreateDraftOrder();
        var item = CreateItem("B01", qty: 0);

        // Act
        var act = () => order.AddItem(item);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenDraft_WhenSubmitWithSameLayananOrderAndTujuan_ThenThrowsInvalidOperationException()
    {
        // AC-04: Unit layanan peminta dan unit tujuan tidak boleh sama
        // Arrange
        var order = CreateDraftOrder(LayananOrder(), LayananOrder());
        order.AddItem(CreateItem());

        // Act
        var act = () => order.Submit(LayananOrder().ToReff(), "user-submit");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tidak boleh sama*");
    }

    [Fact]
    public void GivenDraftWithoutItems_WhenSubmit_ThenThrowsArgumentException()
    {
        // AC-01: Guard minimal 1 item
        // Arrange
        var order = CreateDraftOrder();

        // Act
        var act = () => order.Submit(LayananTujuan().ToReff(), "user-submit");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*minimal 1 item*");
    }

    [Fact]
    public void GivenValidDraft_WhenSubmitWithoutAutoApprove_ThenTransitionsToSubmitted()
    {
        // AC-01: Pengajuan permintaan mutasi dengan prioritas Cito
        // Arrange
        var order = CreateDraftOrder();
        order.AddItem(CreateItem());

        // Act
        order.Submit(
            LayananTujuan().ToReff(),
            userId: "user-submit",
            priority: OrderMutasiPriorityEnum.Cito,
            orderNote: "Kebutuhan Cito IGD",
            autoApprove: false);

        // Assert
        order.State.Should().Be(OrderMutasiStateEnum.Submitted);
        order.Priority.Should().Be(OrderMutasiPriorityEnum.Cito);
        order.OrderNote.Should().Be("Kebutuhan Cito IGD");
        order.LayananTujuan.LayananId.Should().Be("L02");
        order.AuditTrail.Modified.UserId.Should().Be("user-submit");
    }

    [Fact]
    public void GivenValidDraft_WhenSubmitWithAutoApprove_ThenTransitionsDirectlyToApproved()
    {
        // Arrange
        var order = CreateDraftOrder();
        order.AddItem(CreateItem());

        // Act
        order.Submit(
            LayananTujuan().ToReff(),
            userId: "kepala-unit",
            priority: OrderMutasiPriorityEnum.Rutin,
            orderNote: "Auto disetujui",
            autoApprove: true);

        // Assert
        order.State.Should().Be(OrderMutasiStateEnum.Approved);
        order.Approval.UserId.Should().Be("kepala-unit");
    }

    [Fact]
    public void GivenSubmittedOrder_WhenSetItemQtyOrRemoveItem_ThenUpdatesSuccessfully()
    {
        // AC-02: Koreksi saat review Kepala Unit pada status Submitted
        // Arrange
        var order = CreateDraftOrder();
        order.AddItem(CreateItem("B01", qty: 10));
        order.AddItem(CreateItem("B02", qty: 5));
        order.Submit(LayananTujuan().ToReff(), "user-submit");

        // Act - Koreksi kuantitas B01 menjadi 8
        order.SetItemQty(SampleBrg("B01"), 8, "kepala-unit", stokCurrent: 4);

        // Act - Hapus item B02
        order.RemoveItem(SampleBrg("B02"), "kepala-unit");

        // Assert
        order.ListItem.Should().HaveCount(1);
        var b01 = order.ListItem.First();
        b01.Qty.Should().Be(8);
        b01.StokCurrent.Should().Be(4);
        b01.NoUrut.Should().Be(1);
        order.AuditTrail.Modified.UserId.Should().Be("kepala-unit");
    }

    [Fact]
    public void GivenSubmittedOrder_WhenSetItemQtyToZero_ThenItemIsRemoved()
    {
        // Arrange
        var order = CreateDraftOrder();
        order.AddItem(CreateItem("B01", qty: 10));
        order.Submit(LayananTujuan().ToReff(), "user-submit");

        // Act
        order.SetItemQty(SampleBrg("B01"), 0, "kepala-unit");

        // Assert
        order.ListItem.Should().BeEmpty();
    }

    [Fact]
    public void GivenSubmittedOrder_WhenApprove_ThenStateBecomesApproved()
    {
        // AC-02: Persetujuan dokumen permintaan mutasi
        // Arrange
        var order = CreateDraftOrder();
        order.AddItem(CreateItem());
        order.Submit(LayananTujuan().ToReff(), "user-submit");

        // Act
        order.Approve("kepala-unit");

        // Assert
        order.State.Should().Be(OrderMutasiStateEnum.Approved);
        order.Approval.UserId.Should().Be("kepala-unit");
    }

    [Fact]
    public void GivenSubmittedOrder_WhenReject_ThenStateBecomesRejectedWithNote()
    {
        // Arrange
        var order = CreateDraftOrder();
        order.AddItem(CreateItem());
        order.Submit(LayananTujuan().ToReff(), "user-submit");

        // Act
        order.Reject("Stok pengaju masih mencukupi", "kepala-unit");

        // Assert
        order.State.Should().Be(OrderMutasiStateEnum.Rejected);
        order.Rejection.UserId.Should().Be("kepala-unit");
        order.OrderNote.Should().Be("Stok pengaju masih mencukupi");
    }

    [Fact]
    public void GivenApprovedOrder_WhenRecordPartialFulfillment_ThenStateBecomesPartiallyFulfilled()
    {
        // AC-05: Pemenuhan progresif / parsial
        // Arrange
        var order = CreateDraftOrder();
        order.AddItem(CreateItem("B01", qty: 10));
        order.AddItem(CreateItem("B02", qty: 20));
        order.Submit(LayananTujuan().ToReff(), "user-submit", autoApprove: true);

        // Act - Pemenuhan sebagian B01 (6 dari 10)
        order.RecordFulfillment(SampleBrg("B01"), 6, "petugas-gudang");

        // Assert
        order.State.Should().Be(OrderMutasiStateEnum.PartiallyFulfilled);
        var b01 = order.ListItem.First(x => x.Brg.BrgId == "B01");
        b01.QtyTerpenuhi.Should().Be(6);
        b01.SisaBackorder.Should().Be(4);

        var b02 = order.ListItem.First(x => x.Brg.BrgId == "B02");
        b02.QtyTerpenuhi.Should().Be(0);
        b02.SisaBackorder.Should().Be(20);
    }

    [Fact]
    public void GivenPartiallyFulfilledOrder_WhenRemainingFulfilled_ThenStateBecomesCompleted()
    {
        // AC-05: Pemenuhan penuh mengubah state ke Completed
        // Arrange
        var order = CreateDraftOrder();
        order.AddItem(CreateItem("B01", qty: 10));
        order.Submit(LayananTujuan().ToReff(), "user-submit", autoApprove: true);
        order.RecordFulfillment(SampleBrg("B01"), 6, "petugas-gudang");

        // Act - Penuhi sisa 4
        order.RecordFulfillment(SampleBrg("B01"), 4, "petugas-gudang");

        // Assert
        order.State.Should().Be(OrderMutasiStateEnum.Completed);
        var b01 = order.ListItem.First();
        b01.QtyTerpenuhi.Should().Be(10);
        b01.SisaBackorder.Should().Be(0);
    }

    [Fact]
    public void GivenApprovedOrder_WhenFulfillmentExceedsQty_ThenThrowsArgumentException()
    {
        // Arrange
        var order = CreateDraftOrder();
        order.AddItem(CreateItem("B01", qty: 10));
        order.Submit(LayananTujuan().ToReff(), "user-submit", autoApprove: true);

        // Act
        var act = () => order.RecordFulfillment(SampleBrg("B01"), 11, "petugas-gudang");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*melebihi sisa permintaan*");
    }

    [Fact]
    public void GivenPartiallyFulfilledOrder_WhenForceClose_ThenTransitionsToForceClosed()
    {
        // AC-06: Penutupan paksa sisa backorder
        // Arrange
        var order = CreateDraftOrder();
        order.AddItem(CreateItem("B01", qty: 10));
        order.Submit(LayananTujuan().ToReff(), "user-submit", autoApprove: true);
        order.RecordFulfillment(SampleBrg("B01"), 4, "petugas-gudang");

        // Act
        order.ForceClose("Stok distributor kosong nasional", "kepala-gudang");

        // Assert
        order.State.Should().Be(OrderMutasiStateEnum.ForceClosed);
        order.OrderNote.Should().Be("Stok distributor kosong nasional");
        order.AuditTrail.Modified.UserId.Should().Be("kepala-gudang");
    }

    [Fact]
    public void GivenForceCloseWithEmptyReason_WhenExecuted_ThenThrowsArgumentException()
    {
        // Arrange
        var order = CreateDraftOrder();
        order.AddItem(CreateItem("B01", qty: 10));
        order.Submit(LayananTujuan().ToReff(), "user-submit", autoApprove: true);

        // Act
        var act = () => order.ForceClose("   ", "kepala-gudang");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Alasan force close wajib diisi*");
    }

    [Fact]
    public void GivenUnfulfilledApprovedOrder_WhenCancel_ThenTransitionsToCancelled()
    {
        // AC-07: Pembatalan saat belum ada pemenuhan mutasi
        // Arrange
        var order = CreateDraftOrder();
        order.AddItem(CreateItem("B01", qty: 10));
        order.Submit(LayananTujuan().ToReff(), "user-submit", autoApprove: true);

        // Act
        order.Cancel("Unit tidak memerlukan lagi", "user-batal");

        // Assert
        order.State.Should().Be(OrderMutasiStateEnum.Cancelled);
        order.OrderNote.Should().Be("Unit tidak memerlukan lagi");
        order.AuditTrail.IsVoided.Should().BeTrue();
        order.AuditTrail.Voided.UserId.Should().Be("user-batal");
    }

    [Fact]
    public void GivenPartiallyFulfilledOrder_WhenCancel_ThenThrowsInvalidOperationExceptionDirectingToForceClose()
    {
        // AC-07: Tolak Cancel bila ada pemenuhan parsial, arahkan ForceClose
        // Arrange
        var order = CreateDraftOrder();
        order.AddItem(CreateItem("B01", qty: 10));
        order.Submit(LayananTujuan().ToReff(), "user-submit", autoApprove: true);
        order.RecordFulfillment(SampleBrg("B01"), 2, "petugas-gudang");

        // Act
        var act = () => order.Cancel("Batal sisa", "user-batal");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ForceClose*");
    }
}
