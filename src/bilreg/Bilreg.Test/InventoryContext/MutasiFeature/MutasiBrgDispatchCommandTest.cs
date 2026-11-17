using System.Data.SqlClient;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Application.InventoryContext.MutasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public class MutasiBrgDispatchCommandTest
{
    private readonly Mock<IMutasiBrgRepo> _mutasiBrgRepoMock = new();
    private readonly Mock<IMutasiBrgMovementService> _movementServiceMock = new();
    private readonly Mock<ISqlDatabase> _sqlDatabaseMock = new();
    private readonly Mock<IOrderMutasiRepo> _orderMutasiRepoMock = new();

    public MutasiBrgDispatchCommandTest()
    {
        _sqlDatabaseMock
            .Setup(x => x.ExecuteTransaction(It.IsAny<Action<SqlConnection, SqlTransaction>>()))
            .Callback<Action<SqlConnection, SqlTransaction>>(action => action(null!, null!));
    }

    private MutasiBrgDispatchHandler CreateSut() =>
        new(
            _mutasiBrgRepoMock.Object,
            _movementServiceMock.Object,
            _sqlDatabaseMock.Object,
            _orderMutasiRepoMock.Object);

    private static MutasiBrgModel CreateDraftMutasi(
        string id = "MT2600001A",
        string orderMutasiId = "",
        decimal qty = 10m)
    {
        var mutasi = MutasiBrgModel.Create(
            id,
            DateTime.Now,
            new LayananReff("L01", "Apotek Asal"),
            new LayananReff("L02", "Poli Tujuan"),
            orderMutasiId: orderMutasiId,
            dispatchNote: "Test Dispatch",
            userId: "USER01");

        mutasi.AddItem(MutasiBrgItemModel.Create(
            1,
            new BrgReff("B001", "Paracetamol 500mg"),
            batchNo: "BATCH-01",
            expiredDate: "2026-12-31",
            qtyKirim: qty,
            satuan: new SatuanType("STR", "Strip")));

        return mutasi;
    }

    [Fact]
    public async Task Handle_Success_ShouldDispatchMutasi_AndCallStockDeduction_AndSaveChanges()
    {
        // Arrange
        var mutasi = CreateDraftMutasi("MT2600001A");
        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")))
            .Returns(MayBe.From(mutasi));

        var sut = CreateSut();
        var command = new MutasiBrgDispatchCommand("MT2600001A", "USER_DISPATCH");

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        mutasi.State.Should().Be(MutasiBrgStateEnum.Dispatched);
        mutasi.IssuerUserId.Should().Be("USER_DISPATCH");
        _sqlDatabaseMock.Verify(x => x.ExecuteTransaction(It.IsAny<Action<SqlConnection, SqlTransaction>>()), Times.Once);
        _movementServiceMock.Verify(x => x.DeductStock(mutasi, It.IsAny<SqlConnection>(), It.IsAny<SqlTransaction>()), Times.Once);
        _mutasiBrgRepoMock.Verify(x => x.SaveChanges(mutasi), Times.Once);
    }

    [Fact]
    public async Task Handle_Success_WithOrderMutasi_WhenItemsFullyFulfilled_ShouldCompleteOrderMutasi()
    {
        // Arrange
        const string omId = "OM01";
        var mutasi = CreateDraftMutasi("MT2600001A", orderMutasiId: omId, qty: 10m);

        var orderItem = new OrderMutasiItemModel(1, new BrgReff("B001", "Paracetamol 500mg"), 10m, new SatuanType("STR", "Strip"));
        var orderMutasi = OrderMutasiModel.Create(
            LayananType.Default with { LayananId = "L02", LayananName = "Poli Tujuan" },
            LayananType.Default with { LayananId = "L01", LayananName = "Apotek Asal" },
            "Order note",
            "USER_REQ",
            [orderItem]);

        orderMutasi.Submit(new LayananReff("L01", "Apotek Asal"), "USER_REQ");
        orderMutasi.Approve("USER_APPROVER");

        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")))
            .Returns(MayBe.From(mutasi));

        _orderMutasiRepoMock
            .Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == omId)))
            .Returns(MayBe.From(orderMutasi));

        var sut = CreateSut();
        var command = new MutasiBrgDispatchCommand("MT2600001A", "USER_DISPATCH");

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        mutasi.State.Should().Be(MutasiBrgStateEnum.Dispatched);
        orderMutasi.State.Should().Be(OrderMutasiStateEnum.Completed);
        _orderMutasiRepoMock.Verify(x => x.SaveChanges(orderMutasi), Times.Once);
        _mutasiBrgRepoMock.Verify(x => x.SaveChanges(mutasi), Times.Once);
    }

    [Fact]
    public async Task Handle_Success_WithOrderMutasi_WhenItemsPartiallyFulfilled_ShouldKeepOrderMutasiApproved()
    {
        // Arrange
        const string omId = "OM01";
        var mutasi = CreateDraftMutasi("MT2600001A", orderMutasiId: omId, qty: 5m); // only 5 sent

        var orderItem = new OrderMutasiItemModel(1, new BrgReff("B001", "Paracetamol 500mg"), 10m, new SatuanType("STR", "Strip")); // 10 requested
        var orderMutasi = OrderMutasiModel.Create(
            LayananType.Default with { LayananId = "L02", LayananName = "Poli Tujuan" },
            LayananType.Default with { LayananId = "L01", LayananName = "Apotek Asal" },
            "Order note",
            "USER_REQ",
            [orderItem]);

        orderMutasi.Submit(new LayananReff("L01", "Apotek Asal"), "USER_REQ");
        orderMutasi.Approve("USER_APPROVER");

        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")))
            .Returns(MayBe.From(mutasi));

        _orderMutasiRepoMock
            .Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == omId)))
            .Returns(MayBe.From(orderMutasi));

        var sut = CreateSut();
        var command = new MutasiBrgDispatchCommand("MT2600001A", "USER_DISPATCH");

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        mutasi.State.Should().Be(MutasiBrgStateEnum.Dispatched);
        orderMutasi.State.Should().Be(OrderMutasiStateEnum.Approved);
        _orderMutasiRepoMock.Verify(x => x.SaveChanges(orderMutasi), Times.Never);
        _mutasiBrgRepoMock.Verify(x => x.SaveChanges(mutasi), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMutasiNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IMutasiBrgKey>()))
            .Returns(MayBe<MutasiBrgModel>.None);

        var sut = CreateSut();
        var command = new MutasiBrgDispatchCommand("NON_EXISTENT", "USER01");

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*NON_EXISTENT*");
    }

    [Fact]
    public async Task Handle_WhenStateIsNotDraft_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mutasi = CreateDraftMutasi("MT2600001A");
        mutasi.Dispatch("USER_PREV");

        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")))
            .Returns(MayBe.From(mutasi));

        var sut = CreateSut();
        var command = new MutasiBrgDispatchCommand("MT2600001A", "USER01");

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public async Task Handle_WhenMutasiHasNoItems_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mutasi = MutasiBrgModel.Create(
            "MT2600001A",
            DateTime.Now,
            new LayananReff("L01", "Apotek Asal"),
            new LayananReff("L02", "Poli Tujuan"),
            userId: "USER01"); // No items added

        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")))
            .Returns(MayBe.From(mutasi));

        var sut = CreateSut();
        var command = new MutasiBrgDispatchCommand("MT2600001A", "USER01");

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*minimal 1 item*");
    }

    [Fact]
    public async Task Handle_WhenStockDeductionThrows_ShouldPropagateException_AndNotCommit()
    {
        // Arrange
        var mutasi = CreateDraftMutasi("MT2600001A");
        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")))
            .Returns(MayBe.From(mutasi));

        _movementServiceMock
            .Setup(x => x.DeductStock(It.IsAny<MutasiBrgModel>(), It.IsAny<SqlConnection>(), It.IsAny<SqlTransaction>()))
            .Throws(new InvalidOperationException("Stok tidak mencukupi untuk barang 'B001'"));

        var sut = CreateSut();
        var command = new MutasiBrgDispatchCommand("MT2600001A", "USER01");

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Stok tidak mencukupi*");

        _mutasiBrgRepoMock.Verify(x => x.SaveChanges(It.IsAny<MutasiBrgModel>()), Times.Never);
    }

    [Theory]
    [InlineData("", "USER01")]
    [InlineData("   ", "USER01")]
    [InlineData("MT2600001A", "")]
    [InlineData("MT2600001A", "   ")]
    public async Task Handle_WhenInputInvalid_ShouldThrowArgumentException(string mutasiId, string userId)
    {
        // Arrange
        var sut = CreateSut();
        var command = new MutasiBrgDispatchCommand(mutasiId, userId);

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
