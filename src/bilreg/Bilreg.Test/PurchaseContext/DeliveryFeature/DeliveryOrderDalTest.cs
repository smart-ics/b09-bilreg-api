using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryOrderDalTest
{
    private readonly DeliveryOrderDal _sut = new(ConnStringHelper.GetTestEnv());

    private static DeliveryOrderDto Faker()
        => new DeliveryOrderDto(
            DeliveryOrderId: "A",
            DoNo: "B",
            SupplierId: "C",
            SupplierName: "D",
            PoReffId: "E",
            DoDate: new DateTime(2026, 8, 24),
            State: DeliveryOrderStateEnum.Draft,
            Notes: "F",
            CrtUser: "G",
            CrtDate: new DateTime(2026, 8, 24),
            UpdUser: "H",
            UpdDate: new DateTime(2026, 8, 24),
            VodUser: string.Empty,
            VodDate: new DateTime(3000, 1, 1));

    private static IDeliveryOrderKey FakerKey()
        => DeliveryOrderModel.Key("A");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());

        actual.Should().BeEquivalentTo(Faker());
    }
}
