using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryOrderItemDalTest
{
    private readonly DeliveryOrderItemDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<DeliveryOrderItemDto> FakerList() =>
    [
        new DeliveryOrderItemDto(
            DeliveryOrderId: "DLVTEST00001",
            ItemNo: 2,
            BrgId: "BRGTEST000002",
            LayananId: "LY002",
            QtyOrder: 5,
            QtyReceived: 2,
            SatuanId: "BOX",
            Harga: 2000.50m,
            Diskon: 100.25m,
            Tax: 190.03m,
            TglEd: new DateTime(2028, 6, 30),
            NoBatch: "BATCH-002",
            State: DeliveryOrderItemStateEnum.Partial,
            CrtUser: "U001",
            CrtDate: new DateTime(2026, 8, 22, 9, 0, 0),
            UpdUser: "U002",
            UpdDate: new DateTime(2026, 8, 22, 10, 0, 0),
            VodUser: string.Empty,
            VodDate: new DateTime(3000, 1, 1)),
        new DeliveryOrderItemDto(
            DeliveryOrderId: "DLVTEST00001",
            ItemNo: 1,
            BrgId: "BRGTEST000001",
            LayananId: "LY001",
            QtyOrder: 10,
            QtyReceived: 0,
            SatuanId: "PCS",
            Harga: 1000.25m,
            Diskon: 50.10m,
            Tax: 95.02m,
            TglEd: new DateTime(2028, 1, 31),
            NoBatch: "BATCH-001",
            State: DeliveryOrderItemStateEnum.Open,
            CrtUser: "U001",
            CrtDate: new DateTime(2026, 8, 22, 9, 0, 0),
            UpdUser: string.Empty,
            UpdDate: new DateTime(3000, 1, 1),
            VodUser: string.Empty,
            VodDate: new DateTime(3000, 1, 1))
    ];

    private static IDeliveryOrderKey FakerKey() =>
        DeliveryOrderModel.Key("DLVTEST00001");

    [Fact]
    public void InsertList_RoundTrip_InItemNumberOrder()
    {
        DeliveryOrderSchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var inserted = FakerList().ToList();
        var expected = inserted.OrderBy(x => x.ItemNo).ToList();
        _sut.Insert(inserted);

        var actual = _sut.ListData(FakerKey()).ToList();

        actual.Should().Equal(expected);
    }

    [Fact]
    public void Delete_RemovesAllItemsForDeliveryOrder()
    {
        DeliveryOrderSchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        _sut.Insert(FakerList());

        _sut.Delete(FakerKey());
        var actual = _sut.ListData(FakerKey());

        actual.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Insert_EmptyList_DoesNotThrow()
    {
        var action = () => _sut.Insert([]);

        action.Should().NotThrow();
    }
}
