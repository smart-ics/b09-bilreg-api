using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;
using Moq;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryOrderRepoTest
{
    private readonly Mock<IDeliveryOrderDal> _deliveryOrderDalMock = new();
    private readonly Mock<IDeliveryOrderItemDal> _deliveryOrderItemDalMock = new();
    private readonly DeliveryOrderRepo _repository;

    public DeliveryOrderRepoTest()
    {
        _repository = new DeliveryOrderRepo(
            _deliveryOrderDalMock.Object,
            _deliveryOrderItemDalMock.Object);
    }

    private static DeliveryOrderModel Faker()
    {
        var auditTrail = AuditTrailType.Create(
            "G",
            new DateTime(2026, 8, 24));
        var item = new DeliveryOrderItemModel(
            itemNo: 1,
            brgId: "H",
            layananId: "I",
            qtyOrder: 10,
            satuanId: "J",
            harga: 1000,
            diskon: 100,
            tax: 90,
            tglEd: new DateTime(2027, 8, 24),
            noBatch: "K");

        return new DeliveryOrderModel(
            deliveryOrderId: "A",
            doNo: "B",
            supplier: new SupplierReff("C", "D"),
            poReffId: "E",
            doDate: new DateTime(2026, 8, 24),
            state: DeliveryOrderStateEnum.Draft,
            notes: "F",
            auditTrail: auditTrail,
            listItem: [item]);
    }

    [Fact]
    public void UT1_GivenNewDeliveryOrder_WhenSaveChanges_ThenInsertHeaderAndReplaceItems()
    {
        var model = Faker();
        var expectedHeader = DeliveryOrderDto.FromModel(model);
        var expectedItem = DeliveryOrderItemDto.FromModel(model, model.ListItem.Single());
        _deliveryOrderDalMock
            .Setup(x => x.GetData(It.IsAny<IDeliveryOrderKey>()))
            .Returns((DeliveryOrderDto)null!);

        _repository.SaveChanges(model);

        _deliveryOrderDalMock.Verify(
            x => x.GetData(It.Is<IDeliveryOrderKey>(key =>
                key.DeliveryOrderId == model.DeliveryOrderId)),
            Times.Once);
        _deliveryOrderDalMock.Verify(
            x => x.Insert(It.Is<DeliveryOrderDto>(dto => dto == expectedHeader)),
            Times.Once);
        _deliveryOrderDalMock.Verify(
            x => x.Update(It.IsAny<DeliveryOrderDto>()),
            Times.Never);
        _deliveryOrderItemDalMock.Verify(
            x => x.Delete(It.Is<IDeliveryOrderKey>(key =>
                key.DeliveryOrderId == model.DeliveryOrderId)),
            Times.Once);
        _deliveryOrderItemDalMock.Verify(
            x => x.Insert(It.Is<IEnumerable<DeliveryOrderItemDto>>(list =>
                list.Count() == 1 && list.Single() == expectedItem)),
            Times.Once);
    }

    [Fact]
    public void UT2_GivenExistingDeliveryOrder_WhenSaveChanges_ThenUpdateHeaderAndReplaceItems()
    {
        var model = Faker();
        var expectedHeader = DeliveryOrderDto.FromModel(model);
        var expectedItem = DeliveryOrderItemDto.FromModel(model, model.ListItem.Single());
        _deliveryOrderDalMock
            .Setup(x => x.GetData(It.IsAny<IDeliveryOrderKey>()))
            .Returns(expectedHeader);

        _repository.SaveChanges(model);

        _deliveryOrderDalMock.Verify(
            x => x.GetData(It.Is<IDeliveryOrderKey>(key =>
                key.DeliveryOrderId == model.DeliveryOrderId)),
            Times.Once);
        _deliveryOrderDalMock.Verify(
            x => x.Update(It.Is<DeliveryOrderDto>(dto => dto == expectedHeader)),
            Times.Once);
        _deliveryOrderDalMock.Verify(
            x => x.Insert(It.IsAny<DeliveryOrderDto>()),
            Times.Never);
        _deliveryOrderItemDalMock.Verify(
            x => x.Delete(It.Is<IDeliveryOrderKey>(key =>
                key.DeliveryOrderId == model.DeliveryOrderId)),
            Times.Once);
        _deliveryOrderItemDalMock.Verify(
            x => x.Insert(It.Is<IEnumerable<DeliveryOrderItemDto>>(list =>
                list.Count() == 1 && list.Single() == expectedItem)),
            Times.Once);
    }
}
