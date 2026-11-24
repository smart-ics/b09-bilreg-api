using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Application.InventoryContext.MutasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public class OrderMutasiLifecycleCommandsTest
{
    private readonly Mock<IOrderMutasiRepo> _orderMutasiRepoMock = new();
    private readonly Mock<ILayananRepo> _layananRepoMock = new();

    private static readonly LayananType UnitPeminta = LayananType.Default with { LayananId = "LAY01", LayananName = "Unit Peminta" };
    private static readonly LayananType UnitTujuan = LayananType.Default with { LayananId = "LAY02", LayananName = "Unit Tujuan" };

    private static OrderMutasiModel CreateTestOrder(
        string id = "OM001",
        OrderMutasiStateEnum state = OrderMutasiStateEnum.Draft,
        decimal initialQty = 10m,
        decimal fulfilledQty = 0m)
    {
        var item = new OrderMutasiItemModel(
            noUrut: 1,
            brg: new BrgReff("BRG01", "Paracetamol 500mg"),
            qty: initialQty,
            satuan: new SatuanType("SAT01", "Tablet"),
            stokCurrent: 5m,
            qtyTerpenuhi: fulfilledQty);

        var order = OrderMutasiModel.Create(
            layananOrder: UnitPeminta,
            layananTujuan: UnitTujuan,
            orderNote: "Test Note",
            userId: "USER01",
            listItem: [item]);

        // Manipulate internal State if needed
        if (state == OrderMutasiStateEnum.Submitted)
        {
            order.Submit(UnitTujuan.ToReff(), "USER01", OrderMutasiPriorityEnum.Rutin, "Note");
        }
        else if (state == OrderMutasiStateEnum.Approved)
        {
            order.Submit(UnitTujuan.ToReff(), "USER01", OrderMutasiPriorityEnum.Rutin, "Note");
            order.Approve("APPROVER01");
        }
        else if (state == OrderMutasiStateEnum.PartiallyFulfilled)
        {
            order.Submit(UnitTujuan.ToReff(), "USER01", OrderMutasiPriorityEnum.Rutin, "Note");
            order.Approve("APPROVER01");
            order.RecordFulfillment(new BrgKey("BRG01"), fulfilledQty > 0 ? fulfilledQty : 3m, "FULFILLER01");
        }

        return order;
    }

    #region UPDATE ITEM QTY COMMAND TESTS
    [Fact]
    public async Task UpdateItemQty_WhenValid_UpdatesQuantityAndSaves()
    {
        // Arrange
        var order = CreateTestOrder("OM001", OrderMutasiStateEnum.Draft, initialQty: 10m);
        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == "OM001")))
            .Returns(MayBe.From(order));

        var sut = new OrderMutasiUpdateItemQtyHandler(_orderMutasiRepoMock.Object);
        var cmd = new OrderMutasiUpdateItemQtyCommand("OM001", "BRG01", 25m, "USER02");

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        order.ListItem.First(x => x.Brg.BrgId == "BRG01").Qty.Should().Be(25m);
        _orderMutasiRepoMock.Verify(x => x.SaveChanges(order), Times.Once);
    }

    [Fact]
    public async Task UpdateItemQty_WhenQtyZero_RemovesItem()
    {
        // Arrange
        var order = CreateTestOrder("OM001", OrderMutasiStateEnum.Draft, initialQty: 10m);
        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == "OM001")))
            .Returns(MayBe.From(order));

        var sut = new OrderMutasiUpdateItemQtyHandler(_orderMutasiRepoMock.Object);
        var cmd = new OrderMutasiUpdateItemQtyCommand("OM001", "BRG01", 0m, "USER02");

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        order.ListItem.Should().BeEmpty();
        _orderMutasiRepoMock.Verify(x => x.SaveChanges(order), Times.Once);
    }

    [Fact]
    public async Task UpdateItemQty_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderMutasiKey>()))
            .Returns(MayBe<OrderMutasiModel>.None);

        var sut = new OrderMutasiUpdateItemQtyHandler(_orderMutasiRepoMock.Object);
        var cmd = new OrderMutasiUpdateItemQtyCommand("NOT_FOUND", "BRG01", 5m, "USER01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
    #endregion

    #region REMOVE ITEM COMMAND TESTS
    [Fact]
    public async Task RemoveItem_WhenValid_RemovesItemAndSaves()
    {
        // Arrange
        var order = CreateTestOrder("OM001", OrderMutasiStateEnum.Draft);
        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == "OM001")))
            .Returns(MayBe.From(order));

        var sut = new OrderMutasiRemoveItemHandler(_orderMutasiRepoMock.Object);
        var cmd = new OrderMutasiRemoveItemCommand("OM001", "BRG01", "USER01");

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        order.ListItem.Should().BeEmpty();
        _orderMutasiRepoMock.Verify(x => x.SaveChanges(order), Times.Once);
    }

    [Fact]
    public async Task RemoveItem_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderMutasiKey>()))
            .Returns(MayBe<OrderMutasiModel>.None);

        var sut = new OrderMutasiRemoveItemHandler(_orderMutasiRepoMock.Object);
        var cmd = new OrderMutasiRemoveItemCommand("NOT_FOUND", "BRG01", "USER01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
    #endregion

    #region SUBMIT COMMAND TESTS
    [Fact]
    public async Task Submit_WhenValidRutin_SetsSubmittedStateAndPriority()
    {
        // Arrange
        var order = CreateTestOrder("OM001", OrderMutasiStateEnum.Draft);
        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == "OM001")))
            .Returns(MayBe.From(order));
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "LAY02")))
            .Returns(MayBe.From(UnitTujuan));

        var sut = new OrderMutasiSubmitHandler(_orderMutasiRepoMock.Object, _layananRepoMock.Object);
        var cmd = new OrderMutasiSubmitCommand("OM001", "LAY02", "USER01", OrderMutasiPriorityEnum.Rutin, "Catatan Rutin", false);

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        order.State.Should().Be(OrderMutasiStateEnum.Submitted);
        order.Priority.Should().Be(OrderMutasiPriorityEnum.Rutin);
        order.OrderNote.Should().Be("Catatan Rutin");
        _orderMutasiRepoMock.Verify(x => x.SaveChanges(order), Times.Once);
    }

    [Fact]
    public async Task Submit_WhenAutoApproveTrue_SetsApprovedStateDirectly()
    {
        // Arrange
        var order = CreateTestOrder("OM001", OrderMutasiStateEnum.Draft);
        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == "OM001")))
            .Returns(MayBe.From(order));
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "LAY02")))
            .Returns(MayBe.From(UnitTujuan));

        var sut = new OrderMutasiSubmitHandler(_orderMutasiRepoMock.Object, _layananRepoMock.Object);
        var cmd = new OrderMutasiSubmitCommand("OM001", "LAY02", "SUPERVISOR01", OrderMutasiPriorityEnum.Cito, "Urgensi Cito", true);

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        order.State.Should().Be(OrderMutasiStateEnum.Approved);
        order.Priority.Should().Be(OrderMutasiPriorityEnum.Cito);
        order.Approval.Should().NotBeNull();
        order.Approval!.UserId.Should().Be("SUPERVISOR01");
        _orderMutasiRepoMock.Verify(x => x.SaveChanges(order), Times.Once);
    }

    [Fact]
    public async Task Submit_WhenLayananTujuanEqualsLayananOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = CreateTestOrder("OM001", OrderMutasiStateEnum.Draft);
        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == "OM001")))
            .Returns(MayBe.From(order));
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "LAY01")))
            .Returns(MayBe.From(UnitPeminta));

        var sut = new OrderMutasiSubmitHandler(_orderMutasiRepoMock.Object, _layananRepoMock.Object);
        var cmd = new OrderMutasiSubmitCommand("OM001", "LAY01", "USER01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak boleh sama*");
    }

    [Fact]
    public async Task Submit_WhenLayananTujuanNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var order = CreateTestOrder("OM001", OrderMutasiStateEnum.Draft);
        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == "OM001")))
            .Returns(MayBe.From(order));
        _layananRepoMock.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>()))
            .Returns(MayBe<LayananType>.None);

        var sut = new OrderMutasiSubmitHandler(_orderMutasiRepoMock.Object, _layananRepoMock.Object);
        var cmd = new OrderMutasiSubmitCommand("OM001", "UNKNOWN", "USER01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*LayananTujuan UNKNOWN not found*");
    }
    #endregion

    #region CANCEL COMMAND TESTS
    [Fact]
    public async Task Cancel_WhenDraftOrSubmittedOrApprovedWithoutFulfillment_CancelsSuccessfully()
    {
        // Arrange
        var order = CreateTestOrder("OM001", OrderMutasiStateEnum.Approved, initialQty: 10m, fulfilledQty: 0m);
        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == "OM001")))
            .Returns(MayBe.From(order));

        var sut = new OrderMutasiCancelHandler(_orderMutasiRepoMock.Object);
        var cmd = new OrderMutasiCancelCommand("OM001", "Alasan pembatalan", "USER01");

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        order.State.Should().Be(OrderMutasiStateEnum.Cancelled);
        order.OrderNote.Should().Be("Alasan pembatalan");
        _orderMutasiRepoMock.Verify(x => x.SaveChanges(order), Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenOrderHasPartialFulfillment_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = CreateTestOrder("OM001", OrderMutasiStateEnum.PartiallyFulfilled, initialQty: 10m, fulfilledQty: 3m);
        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == "OM001")))
            .Returns(MayBe.From(order));

        var sut = new OrderMutasiCancelHandler(_orderMutasiRepoMock.Object);
        var cmd = new OrderMutasiCancelCommand("OM001", "Batal", "USER01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ForceClose*");
    }
    #endregion

    #region FORCE CLOSE COMMAND TESTS
    [Fact]
    public async Task ForceClose_WhenApprovedOrPartiallyFulfilled_ForcesCloseSuccessfully()
    {
        // Arrange
        var order = CreateTestOrder("OM001", OrderMutasiStateEnum.PartiallyFulfilled, initialQty: 10m, fulfilledQty: 3m);
        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == "OM001")))
            .Returns(MayBe.From(order));

        var sut = new OrderMutasiForceCloseHandler(_orderMutasiRepoMock.Object);
        var cmd = new OrderMutasiForceCloseCommand("OM001", "Stok distributor kosong, sisa backorder ditutup", "USER01");

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        order.State.Should().Be(OrderMutasiStateEnum.ForceClosed);
        order.OrderNote.Should().Be("Stok distributor kosong, sisa backorder ditutup");
        _orderMutasiRepoMock.Verify(x => x.SaveChanges(order), Times.Once);
    }

    [Fact]
    public async Task ForceClose_WhenInDraftState_ThrowsArgumentException()
    {
        // Arrange
        var order = CreateTestOrder("OM001", OrderMutasiStateEnum.Draft);
        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == "OM001")))
            .Returns(MayBe.From(order));

        var sut = new OrderMutasiForceCloseHandler(_orderMutasiRepoMock.Object);
        var cmd = new OrderMutasiForceCloseCommand("OM001", "Reason", "USER01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Status invalid*");
    }

    [Fact]
    public async Task ForceClose_WhenReasonEmpty_ThrowsArgumentException()
    {
        // Arrange
        var order = CreateTestOrder("OM001", OrderMutasiStateEnum.Approved);
        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == "OM001")))
            .Returns(MayBe.From(order));

        var sut = new OrderMutasiForceCloseHandler(_orderMutasiRepoMock.Object);
        var cmd = new OrderMutasiForceCloseCommand("OM001", "  ", "USER01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
    #endregion
}
