using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryOrderDalTest
{
    private readonly DeliveryOrderDal _sut = new(ConnStringHelper.GetTestEnv());

    private static DeliveryOrderDto FakerData() => new(
        DeliveryOrderId: "DLVTEST00001",
        DoNo: "DOTEST0001",
        SupplierId: "SUPTEST00001",
        SupplierName: "Supplier Test",
        PoReffId: "POTEST0001",
        DoDate: new DateTime(2026, 8, 22, 10, 0, 0),
        State: DeliveryOrderStateEnum.Draft,
        Notes: "Delivery order test",
        CrtUser: "U001",
        CrtDate: new DateTime(2026, 8, 22, 9, 0, 0),
        UpdUser: string.Empty,
        UpdDate: new DateTime(3000, 1, 1),
        VodUser: string.Empty,
        VodDate: new DateTime(3000, 1, 1));

    private static IDeliveryOrderKey FakerKey() =>
        DeliveryOrderModel.Key("DLVTEST00001");

    [Fact]
    public void InsertGet_RoundTrip()
    {
        DeliveryOrderSchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var expected = FakerData();
        _sut.Insert(expected);

        var actual = _sut.GetData(FakerKey());

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UpdateGet_RoundTrip()
    {
        DeliveryOrderSchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var original = FakerData();
        var expected = original with
        {
            SupplierId = "SUPTEST00002",
            SupplierName = "Updated Supplier",
            PoReffId = "POTEST0002",
            DoDate = new DateTime(2026, 8, 23, 11, 30, 0),
            State = DeliveryOrderStateEnum.Open,
            Notes = "Updated delivery order",
            UpdUser = "U002",
            UpdDate = new DateTime(2026, 8, 23, 11, 31, 0),
            VodUser = "U003",
            VodDate = new DateTime(2026, 8, 23, 12, 0, 0)
        };
        _sut.Insert(original);

        _sut.Update(expected);
        var actual = _sut.GetData(FakerKey());

        actual.Should().BeEquivalentTo(expected);
    }
}
