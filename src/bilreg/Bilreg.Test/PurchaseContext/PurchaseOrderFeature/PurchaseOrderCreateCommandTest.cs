using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Application.PurchaseContext.PurchaseOrderFeature.UseCases;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.DataTypeExtension;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Test.PurchaseContext.PurchaseOrderFeature;

public class PurchaseOrderCreateCommandTest
{
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly Mock<IPartnerRepo> _partnerRepoMock = new();
    private readonly Mock<IBrgRepo> _brgRepoMock = new();
    private readonly Mock<ISatuanRepo> _satuanRepoMock = new();
    private readonly Mock<IPurchaseOrderRepo> _purchaseOrderRepoMock = new();

    private PurchaseOrderCreateHandler CreateSut() =>
        new(_tglJamProviderMock.Object,
            _partnerRepoMock.Object,
            _brgRepoMock.Object,
            _satuanRepoMock.Object,
            _purchaseOrderRepoMock.Object);

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
    public async Task Handle_WhenBarangIsActive_CreatesPurchaseOrderSuccessfully()
    {
        // Arrange
        var partner = new PartnerType("P001", "PT Kimia Sehat");
        var activeBrg = CreateMockBrg("B001", "Amoxicillin 500mg", isAktif: true);
        var satuan = new SatuanType("S001", "Strip");

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 12, 10, 0, 0));
        _partnerRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPartnerKey>())).Returns(MayBe.From(partner));
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>())).Returns(MayBe.From(activeBrg));
        _satuanRepoMock.Setup(x => x.LoadEntity(It.IsAny<ISatuanKey>())).Returns(MayBe.From(satuan));

        var sut = CreateSut();
        var command = new PurchaseOrderCreateCommand(
            "P001",
            "USER01",
            "PO Rutin",
            [new PurchaseOrderCreateItemCommand("B001", "S001", 10000m, 5m, 5m, 0m, 0m, 0m)]
        );

        // Act
        var response = await sut.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.PurchaseOrderId.Should().NotBeNullOrWhiteSpace();
        _purchaseOrderRepoMock.Verify(x => x.SaveChanges(It.Is<PurchaseOrderModel>(po =>
            po.Partner.PartnerId == "P001" &&
            po.ListItem.Count() == 1 &&
            po.ListItem.First().Brg.BrgId == "B001"
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBarangIsInactive_ThrowsInvalidOperationException()
    {
        // Arrange (AC-05 Downstream Inactive Status Protection Guard)
        var partner = new PartnerType("P001", "PT Kimia Sehat");
        var inactiveBrg = CreateMockBrg("B002", "Paracetamol Non-Aktif", isAktif: false);
        var satuan = new SatuanType("S001", "Strip");

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 12, 10, 0, 0));
        _partnerRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPartnerKey>())).Returns(MayBe.From(partner));
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>())).Returns(MayBe.From(inactiveBrg));
        _satuanRepoMock.Setup(x => x.LoadEntity(It.IsAny<ISatuanKey>())).Returns(MayBe.From(satuan));

        var sut = CreateSut();
        var command = new PurchaseOrderCreateCommand(
            "P001",
            "USER01",
            "PO Rutin",
            [new PurchaseOrderCreateItemCommand("B002", "S001", 10000m, 5m, 5m, 0m, 0m, 0m)]
        );

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("Barang 'Paracetamol Non-Aktif' (B002) berstatus Non-Aktif.");
        _purchaseOrderRepoMock.Verify(x => x.SaveChanges(It.IsAny<PurchaseOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBarangNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var partner = new PartnerType("P001", "PT Kimia Sehat");

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 12, 10, 0, 0));
        _partnerRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPartnerKey>())).Returns(MayBe.From(partner));
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>())).Returns(MayBe<IBrg>.None);

        var sut = CreateSut();
        var command = new PurchaseOrderCreateCommand(
            "P001",
            "USER01",
            "PO Rutin",
            [new PurchaseOrderCreateItemCommand("B999", "S001", 10000m, 5m, 5m, 0m, 0m, 0m)]
        );

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Barang B999 not found*");
        _purchaseOrderRepoMock.Verify(x => x.SaveChanges(It.IsAny<PurchaseOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSatuanNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var partner = new PartnerType("P001", "PT Kimia Sehat");
        var activeBrg = CreateMockBrg("B001", "Amoxicillin 500mg", isAktif: true);

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 12, 10, 0, 0));
        _partnerRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPartnerKey>())).Returns(MayBe.From(partner));
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>())).Returns(MayBe.From(activeBrg));
        _satuanRepoMock.Setup(x => x.LoadEntity(It.IsAny<ISatuanKey>())).Returns(MayBe<SatuanType>.None);

        var sut = CreateSut();
        var command = new PurchaseOrderCreateCommand(
            "P001",
            "USER01",
            "PO Rutin",
            [new PurchaseOrderCreateItemCommand("B001", "S999", 10000m, 5m, 5m, 0m, 0m, 0m)]
        );

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Satuan Barang S999 not found*");
        _purchaseOrderRepoMock.Verify(x => x.SaveChanges(It.IsAny<PurchaseOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPartnerNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 12, 10, 0, 0));
        _partnerRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPartnerKey>())).Returns(MayBe<PartnerType>.None);

        var sut = CreateSut();
        var command = new PurchaseOrderCreateCommand(
            "P999",
            "USER01",
            "PO Rutin",
            [new PurchaseOrderCreateItemCommand("B001", "S001", 10000m, 5m, 5m, 0m, 0m, 0m)]
        );

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Partner with Id: P999 not found.*");
        _purchaseOrderRepoMock.Verify(x => x.SaveChanges(It.IsAny<PurchaseOrderModel>()), Times.Never);
    }

    [Theory]
    [InlineData(null, "USER01")]
    [InlineData("", "USER01")]
    [InlineData("   ", "USER01")]
    [InlineData("P001", null)]
    [InlineData("P001", "")]
    [InlineData("P001", "   ")]
    public async Task Handle_WhenPartnerOrUserIdEmpty_ThrowsArgumentException(string? partnerId, string? userId)
    {
        var sut = CreateSut();
        var command = new PurchaseOrderCreateCommand(
            partnerId!,
            userId!,
            "PO Rutin",
            [new PurchaseOrderCreateItemCommand("B001", "S001", 10000m, 5m, 5m, 0m, 0m, 0m)]
        );

        var act = () => sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenListItemIsEmpty_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var command = new PurchaseOrderCreateCommand(
            "P001",
            "USER01",
            "PO Rutin",
            []
        );

        var act = () => sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*List Item tidak boleh kosong*");
    }
}
