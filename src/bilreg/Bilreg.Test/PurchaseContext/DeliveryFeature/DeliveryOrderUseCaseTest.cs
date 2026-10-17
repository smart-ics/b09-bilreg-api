using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.PurchaseContext.DeliveryFeature;
using Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryOrderUseCaseTest
{
    private static readonly DateTime Now = new(2026, 8, 24, 10, 30, 0);
    private readonly Mock<IDeliveryOrderRepo> _deliveryOrderRepo = new();
    private readonly Mock<IBrgRepo> _brgRepo = new();
    private readonly Mock<ISatuanRepo> _satuanRepo = new();
    private readonly Mock<ILayananRepo> _layananRepo = new();
    private readonly TestTglJamProvider _clock = new(Now);

    public DeliveryOrderUseCaseTest()
    {
        var brg = new Mock<IBrg>();
        brg.SetupGet(x => x.BrgId).Returns("BRG1");
        brg.SetupGet(x => x.ListSatuan).Returns(
            [new BrgSatuanType(new SatuanType("SAT1", "Box"), 1)]);
        _brgRepo.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe.From(brg.Object));
        _satuanRepo.Setup(x => x.LoadEntity(It.IsAny<ISatuanKey>()))
            .Returns(MayBe.From(new SatuanType("SAT1", "Box")));
        _layananRepo.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>()))
            .Returns(MayBe.From(LayananType.Default with { LayananId = "LYN1" }));
    }

    [Fact]
    public async Task Create_ValidDraft_SavesNumberedAggregateAndReturnsIdentity()
    {
        DeliveryOrderModel? saved = null;
        _deliveryOrderRepo.Setup(x => x.SaveChanges(It.IsAny<DeliveryOrderModel>()))
            .Callback<DeliveryOrderModel>(x => saved = x);
        var sut = new DeliveryOrderCreateHandler(
            _deliveryOrderRepo.Object, _brgRepo.Object, _satuanRepo.Object,
            _layananRepo.Object, _clock);
        var cmd = new DeliveryOrderCreateCmd(
            "DO-1", "SUP1", "Supplier", null, Now.Date, "Notes", "USR1",
            [Item()]);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.DeliveryOrderId.Should().StartWith("DLV");
        result.State.Should().Be(DeliveryOrderStateEnum.Draft);
        saved.Should().NotBeNull();
        saved!.ListItem.Single().ItemNo.Should().Be(1);
        saved.AuditTrail.Created.Should().Be(new AuditInfoType("USR1", Now));
    }

    [Fact]
    public async Task Create_DuplicateDoNo_ThrowsWithoutSaving()
    {
        _deliveryOrderRepo.Setup(x => x.IsDoNoExist("DO-1")).Returns(true);
        var sut = new DeliveryOrderCreateHandler(
            _deliveryOrderRepo.Object, _brgRepo.Object, _satuanRepo.Object,
            _layananRepo.Object, _clock);
        var cmd = new DeliveryOrderCreateCmd(
            "DO-1", "SUP1", "Supplier", null, Now.Date, "", "USR1", []);

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah digunakan*");
        _deliveryOrderRepo.Verify(x => x.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Create_UnsupportedProductUnit_Throws()
    {
        _satuanRepo.Setup(x => x.LoadEntity(It.IsAny<ISatuanKey>()))
            .Returns(MayBe.From(new SatuanType("SAT2", "Each")));
        var sut = new DeliveryOrderCreateHandler(
            _deliveryOrderRepo.Object, _brgRepo.Object, _satuanRepo.Object,
            _layananRepo.Object, _clock);
        var cmd = new DeliveryOrderCreateCmd(
            "DO-1", "SUP1", "Supplier", null, Now.Date, "", "USR1",
            [Item(satuanId: "SAT2")]);

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak terdaftar*");
    }

    [Fact]
    public async Task Get_ExistingOrder_MapsHeaderAuditAndItems()
    {
        var order = Order([DomainItem(1)]);
        _deliveryOrderRepo.Setup(x => x.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe.From(order));
        var sut = new DeliveryOrderGetHandler(_deliveryOrderRepo.Object);

        var result = await sut.Handle(
            new DeliveryOrderGetQry(order.DeliveryOrderId), CancellationToken.None);

        result.DoNo.Should().Be("DO-1");
        result.SupplierId.Should().Be("SUP1");
        result.ListItem.Should().ContainSingle();
        result.ListItem.Single().QtyRemaining.Should().Be(10);
        result.Audit.CrtUser.Should().Be("CREATE");
    }

    [Fact]
    public async Task UpdateHeader_Draft_UpdatesSnapshotAndAudit()
    {
        var order = Order([]);
        SetupOrder(order);
        var sut = new DeliveryOrderUpdateHeaderHandler(_deliveryOrderRepo.Object, _clock);

        await sut.Handle(new DeliveryOrderUpdateHeaderCmd(
            order.DeliveryOrderId, "SUP2", "Supplier Dua", "PO-2", "Updated", "USR2"),
            CancellationToken.None);

        order.Supplier.Should().Be(new SupplierReff("SUP2", "Supplier Dua"));
        order.PoReffId.Should().Be("PO-2");
        order.Notes.Should().Be("Updated");
        order.AuditTrail.Modified.Should().Be(new AuditInfoType("USR2", Now));
        _deliveryOrderRepo.Verify(x => x.SaveChanges(order), Times.Once);
    }

    [Fact]
    public async Task AddItem_Draft_AddsItemAndAudit()
    {
        var order = Order([]);
        SetupOrder(order);
        var sut = new DeliveryOrderAddItemHandler(
            _deliveryOrderRepo.Object, _brgRepo.Object, _satuanRepo.Object,
            _layananRepo.Object, _clock);

        await sut.Handle(
            new DeliveryOrderAddItemCmd(order.DeliveryOrderId, "USR2", Item()),
            CancellationToken.None);

        order.ListItem.Should().ContainSingle();
        order.ListItem.Single().ItemNo.Should().Be(1);
        order.AuditTrail.Modified.Should().Be(new AuditInfoType("USR2", Now));
    }

    [Fact]
    public async Task RemoveItem_Draft_RemovesRenumbersAndAudits()
    {
        var order = Order([DomainItem(1), DomainItem(2, "BRG2")]);
        SetupOrder(order);
        var sut = new DeliveryOrderRemoveItemHandler(_deliveryOrderRepo.Object, _clock);

        await sut.Handle(
            new DeliveryOrderRemoveItemCmd(order.DeliveryOrderId, 1, "USR2"),
            CancellationToken.None);

        order.ListItem.Should().ContainSingle();
        order.ListItem.Single().ItemNo.Should().Be(1);
        order.ListItem.Single().BrgId.Should().Be("BRG2");
        order.AuditTrail.Modified.Should().Be(new AuditInfoType("USR2", Now));
    }

    [Fact]
    public async Task UpdateHeader_NonDraft_ThrowsWithoutSaving()
    {
        var order = Order([], DeliveryOrderStateEnum.Open);
        SetupOrder(order);
        var sut = new DeliveryOrderUpdateHeaderHandler(_deliveryOrderRepo.Object, _clock);

        var act = async () => await sut.Handle(new DeliveryOrderUpdateHeaderCmd(
            order.DeliveryOrderId, "SUP2", "Supplier Dua", null, "", "USR2"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _deliveryOrderRepo.Verify(x => x.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Never);
    }

    private void SetupOrder(DeliveryOrderModel order) =>
        _deliveryOrderRepo.Setup(x => x.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe.From(order));

    private static DeliveryOrderDraftItemRequest Item(string satuanId = "SAT1") =>
        new("BRG1", "LYN1", 10, satuanId, 100, 10, 11);

    private static DeliveryOrderItemModel DomainItem(int itemNo, string brgId = "BRG1") =>
        new(itemNo, brgId, "LYN1", 10, "SAT1", 100, 10, 11,
            new DateTime(3000, 1, 1), "");

    private static DeliveryOrderModel Order(
        IEnumerable<DeliveryOrderItemModel> items,
        DeliveryOrderStateEnum state = DeliveryOrderStateEnum.Draft) =>
        new("DLV1", "DO-1", new SupplierReff("SUP1", "Supplier"), "", Now.Date,
            state, "Notes", AuditTrailType.Create("CREATE", Now.AddDays(-1)), items);
}
