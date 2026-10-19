using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Application.InventoryContext.MutasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.DataTypeExtension;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public class OrderMutasiAddItemCommandTest
{
    private readonly Mock<IOrderMutasiRepo> _orderMutasiRepoMock = new();
    private readonly Mock<IBrgRepo> _brgRepoMock = new();
    private readonly Mock<ISatuanRepo> _satuanRepoMock = new();
    private readonly Mock<ILayananRepo> _layananRepoMock = new();

    private OrderMutasiAddItemHandler CreateSut() =>
        new(_orderMutasiRepoMock.Object,
            _brgRepoMock.Object,
            _satuanRepoMock.Object,
            _layananRepoMock.Object);

    private static IBrg CreateMockBrg(string id, string name, bool isAktif)
    {
        var mock = new Mock<IBrg>();
        mock.Setup(x => x.BrgId).Returns(id);
        mock.Setup(x => x.BrgName).Returns(name);
        mock.Setup(x => x.IsAktif).Returns(isAktif);
        mock.Setup(x => x.ToReff()).Returns(new BrgReff(id, name));
        return mock.Object;
    }

    [Fact]
    public async Task Handle_WhenBarangIsActive_AddsItemSuccessfully()
    {
        // Arrange
        var activeBrg = CreateMockBrg("B001", "Amoxicillin 500mg", isAktif: true);
        var satuan = new SatuanType("S001", "Strip");
        var layanan = LayananType.Default with { LayananId = "L001", LayananName = "Farmasi Rawat Jalan" };

        _orderMutasiRepoMock.Setup(x => x.ListDraftState()).Returns(new List<OrderMutasiHeaderView>());
        _layananRepoMock.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>())).Returns(MayBe.From(layanan));
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>())).Returns(MayBe.From(activeBrg));
        _satuanRepoMock.Setup(x => x.LoadEntity(It.IsAny<ISatuanKey>())).Returns(MayBe.From(satuan));

        var sut = CreateSut();
        var command = new OrderMutasiAddItemCommand("B001", 10, "S001", "L001", "USER01");

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        _orderMutasiRepoMock.Verify(x => x.SaveChanges(It.Is<OrderMutasiModel>(m =>
            m.ListItem.Count() == 1 &&
            m.ListItem.First().Brg.BrgId == "B001" &&
            m.ListItem.First().Qty == 10
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBarangIsInactive_ThrowsInvalidOperationException()
    {
        // Arrange (AC-05 Downstream Inactive Status Protection Guard)
        var inactiveBrg = CreateMockBrg("B002", "Paracetamol Non-Aktif", isAktif: false);
        var satuan = new SatuanType("S001", "Strip");
        var layanan = LayananType.Default with { LayananId = "L001", LayananName = "Farmasi Rawat Jalan" };

        _orderMutasiRepoMock.Setup(x => x.ListDraftState()).Returns(new List<OrderMutasiHeaderView>());
        _layananRepoMock.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>())).Returns(MayBe.From(layanan));
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>())).Returns(MayBe.From(inactiveBrg));
        _satuanRepoMock.Setup(x => x.LoadEntity(It.IsAny<ISatuanKey>())).Returns(MayBe.From(satuan));

        var sut = CreateSut();
        var command = new OrderMutasiAddItemCommand("B002", 10, "S001", "L001", "USER01");

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("Barang 'Paracetamol Non-Aktif' (B002) berstatus Non-Aktif.");
        _orderMutasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<OrderMutasiModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBarangNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var layanan = LayananType.Default with { LayananId = "L001", LayananName = "Farmasi Rawat Jalan" };

        _orderMutasiRepoMock.Setup(x => x.ListDraftState()).Returns(new List<OrderMutasiHeaderView>());
        _layananRepoMock.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>())).Returns(MayBe.From(layanan));
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>())).Returns(MayBe<IBrg>.None);

        var sut = CreateSut();
        var command = new OrderMutasiAddItemCommand("B999", 10, "S001", "L001", "USER01");

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*BrgId B999 not found*");
        _orderMutasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<OrderMutasiModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSatuanNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var activeBrg = CreateMockBrg("B001", "Amoxicillin 500mg", isAktif: true);
        var layanan = LayananType.Default with { LayananId = "L001", LayananName = "Farmasi Rawat Jalan" };

        _orderMutasiRepoMock.Setup(x => x.ListDraftState()).Returns(new List<OrderMutasiHeaderView>());
        _layananRepoMock.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>())).Returns(MayBe.From(layanan));
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>())).Returns(MayBe.From(activeBrg));
        _satuanRepoMock.Setup(x => x.LoadEntity(It.IsAny<ISatuanKey>())).Returns(MayBe<SatuanType>.None);

        var sut = CreateSut();
        var command = new OrderMutasiAddItemCommand("B001", 10, "S999", "L001", "USER01");

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*SatuanId S999 not found*");
        _orderMutasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<OrderMutasiModel>()), Times.Never);
    }

    [Theory]
    [InlineData(null, "S001", "L001", 5)]
    [InlineData("", "S001", "L001", 5)]
    [InlineData("   ", "S001", "L001", 5)]
    [InlineData("B001", null, "L001", 5)]
    [InlineData("B001", "", "L001", 5)]
    [InlineData("B001", "   ", "L001", 5)]
    [InlineData("B001", "S001", null, 5)]
    [InlineData("B001", "S001", "", 5)]
    [InlineData("B001", "S001", "   ", 5)]
    [InlineData("B001", "S001", "L001", 0)]
    [InlineData("B001", "S001", "L001", -1)]
    public async Task Handle_WhenParametersInvalid_ThrowsArgumentException(
        string? brgId, string? satuanId, string? layananOrderId, int qty)
    {
        var sut = CreateSut();
        var command = new OrderMutasiAddItemCommand(brgId!, qty, satuanId!, layananOrderId!, "USER01");

        var act = () => sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
