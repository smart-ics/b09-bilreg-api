using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryOrderItemDalTest
{
    private readonly DeliveryOrderItemDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<DeliveryOrderItemDto> FakerList()
        => new List<DeliveryOrderItemDto>
        {
            new DeliveryOrderItemDto(
                DeliveryOrderId: "A",
                ItemNo: 1,
                BrgId: "B",
                LayananId: "C",
                QtyOrder: 10,
                QtyReceived: 5,
                SatuanId: "D",
                Harga: 1000,
                Diskon: 100,
                Tax: 90,
                TglEd: new DateTime(2027, 8, 24),
                NoBatch: "E",
                State: DeliveryOrderItemStateEnum.Partial,
                CrtUser: "F",
                CrtDate: new DateTime(2026, 8, 24),
                UpdUser: "G",
                UpdDate: new DateTime(2026, 8, 24),
                VodUser: string.Empty,
                VodDate: new DateTime(3000, 1, 1)),
            new DeliveryOrderItemDto(
                DeliveryOrderId: "A",
                ItemNo: 2,
                BrgId: "H",
                LayananId: "I",
                QtyOrder: 20,
                QtyReceived: 20,
                SatuanId: "J",
                Harga: 2000,
                Diskon: 200,
                Tax: 180,
                TglEd: new DateTime(2027, 8, 24),
                NoBatch: "K",
                State: DeliveryOrderItemStateEnum.Received,
                CrtUser: "F",
                CrtDate: new DateTime(2026, 8, 24),
                UpdUser: "G",
                UpdDate: new DateTime(2026, 8, 24),
                VodUser: string.Empty,
                VodDate: new DateTime(3000, 1, 1))
        };

    private static IDeliveryOrderKey FakerKey()
        => DeliveryOrderModel.Key("A");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
        var actual = _sut.ListData(FakerKey());

        actual.Should().BeEquivalentTo(FakerList());
    }
}
