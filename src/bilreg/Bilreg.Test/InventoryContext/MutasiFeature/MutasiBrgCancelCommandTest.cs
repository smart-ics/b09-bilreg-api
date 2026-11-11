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

public class MutasiBrgCancelCommandTest
{
    private readonly Mock<IMutasiBrgRepo> _mutasiBrgRepoMock = new();
    private readonly Mock<IMutasiBrgMovementService> _movementServiceMock = new();
    private readonly Mock<ISqlDatabase> _sqlDatabaseMock = new();
    private readonly Mock<IOrderMutasiRepo> _orderMutasiRepoMock = new();

    public MutasiBrgCancelCommandTest()
    {
        _sqlDatabaseMock
            .Setup(x => x.ExecuteTransaction(It.IsAny<Action<SqlConnection, SqlTransaction>>()))
            .Callback<Action<SqlConnection, SqlTransaction>>(action => action(null!, null!));
    }

    private MutasiBrgCancelHandler CreateSut() =>
        new(
            _mutasiBrgRepoMock.Object,
            _movementServiceMock.Object,
            _sqlDatabaseMock.Object,
            _orderMutasiRepoMock.Object);

    private static MutasiBrgModel CreateDispatchedMutasi(
        string id = "MT2600001A",
        string userId = "USER_DISPATCH",
        string orderMutasiId = "")
    {
        var mutasi = MutasiBrgModel.Create(
            id,
            DateTime.Now,
            new LayananReff("L01", "Apotek Asal"),
            new LayananReff("L02", "Poli Tujuan"),
            orderMutasiId: orderMutasiId,
            userId: "USER01");

        mutasi.AddItem(MutasiBrgItemModel.Create(
            1,
            new BrgReff("B001", "Paracetamol 500mg"),
            batchNo: "BATCH-01",
            expiredDate: "2026-12-31",
            qtyKirim: 10m,
            satuan: new SatuanType("STR", "Strip")));

        mutasi.Dispatch(userId);
        return mutasi;
    }

    [Fact]
    public async Task Handle_Success_ShouldCancelMutasi_AndCallRestoreStock_AndSaveChanges()
    {
        // Arrange
        var mutasi = CreateDispatchedMutasi("MT2600001A");
        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")))
            .Returns(MayBe.From(mutasi));

        var sut = CreateSut();
        var command = new MutasiBrgCancelCommand("MT2600001A", "USER_CANCEL", "Salah kirim");

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        mutasi.State.Should().Be(MutasiBrgStateEnum.Cancelled);
        _sqlDatabaseMock.Verify(x => x.ExecuteTransaction(It.IsAny<Action<SqlConnection, SqlTransaction>>()), Times.Once);
        _movementServiceMock.Verify(x => x.RestoreStock(mutasi, It.IsAny<SqlConnection>(), It.IsAny<SqlTransaction>()), Times.Once);
        _mutasiBrgRepoMock.Verify(x => x.SaveChanges(mutasi), Times.Once);
    }

    [Fact]
    public async Task Handle_Success_WithOrderMutasi_WhenOrderWasCompleted_ShouldReopenOrderMutasi()
    {
        // Arrange
        const string omId = "OM01";
        var mutasi = CreateDispatchedMutasi("MT2600001A", orderMutasiId: omId);

        var orderItem = new OrderMutasiItemModel(1, new BrgReff("B001", "Paracetamol 500mg"), 10m, new SatuanType("STR", "Strip"));
        var orderMutasi = OrderMutasiModel.Create(
            LayananType.Default with { LayananId = "L02", LayananName = "Poli Tujuan" },
            LayananType.Default with { LayananId = "L01", LayananName = "Apotek Asal" },
            "Order note",
            "USER_REQ",
            [orderItem]);

        orderMutasi.Submit(new LayananReff("L01", "Apotek Asal"), "USER_REQ");
        orderMutasi.Approve("USER_APPROVER");
        orderMutasi.Complete();

        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")))
            .Returns(MayBe.From(mutasi));

        _orderMutasiRepoMock
            .Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == omId)))
            .Returns(MayBe.From(orderMutasi));

        var sut = CreateSut();
        var command = new MutasiBrgCancelCommand("MT2600001A", "USER_CANCEL", "Batalkan kiriman");

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        mutasi.State.Should().Be(MutasiBrgStateEnum.Cancelled);
        orderMutasi.State.Should().Be(OrderMutasiStateEnum.Approved);
        _orderMutasiRepoMock.Verify(x => x.SaveChanges(orderMutasi), Times.Once);
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
        var command = new MutasiBrgCancelCommand("NON_EXISTENT", "USER01");

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*NON_EXISTENT*");
    }

    [Fact]
    public async Task Handle_WhenStateIsNotDispatched_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mutasi = MutasiBrgModel.Create(
            "MT2600001A",
            DateTime.Now,
            new LayananReff("L01", "Apotek Asal"),
            new LayananReff("L02", "Poli Tujuan"),
            userId: "USER01"); // Draft state

        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")))
            .Returns(MayBe.From(mutasi));

        var sut = CreateSut();
        var command = new MutasiBrgCancelCommand("MT2600001A", "USER01");

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Dispatched*");
    }

    [Fact]
    public async Task Handle_WhenRestoreStockThrows_ShouldPropagateException_AndNotCommit()
    {
        // Arrange
        var mutasi = CreateDispatchedMutasi("MT2600001A");
        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")))
            .Returns(MayBe.From(mutasi));

        _movementServiceMock
            .Setup(x => x.RestoreStock(It.IsAny<MutasiBrgModel>(), It.IsAny<SqlConnection>(), It.IsAny<SqlTransaction>()))
            .Throws(new InvalidOperationException("Gagal restore stok"));

        var sut = CreateSut();
        var command = new MutasiBrgCancelCommand("MT2600001A", "USER01");

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Gagal restore stok");

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
        var command = new MutasiBrgCancelCommand(mutasiId, userId);

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
