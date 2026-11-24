using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Infrastructure.InventoryContext.MutasiFeature;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

[Collection("OrderMutasiDatabaseTests")]
public class OrderMutasiItemDalTest
{
    private readonly OrderMutasiItemDal _sut;

    public OrderMutasiItemDalTest()
    {
        OrderMutasiSchemaFixture.EnsureSchema();
        _sut = new(ConnStringHelper.GetTestEnv());
    }

    private static IEnumerable<OrderMutasiItemDto> FakerList(string orderMutasiId = "OM001")
    {
        return new List<OrderMutasiItemDto>
        {
            new OrderMutasiItemDto(
                OrderMutasiId: orderMutasiId,
                NoUrut: 1,
                BrgId: "B001",
                BrgName: "Obat A",
                StokCurrent: 25.5m,
                Qty: 10m,
                QtyTerpenuhi: 3m,
                SatuanId: "S001",
                SatuanName: "Buah"
            ),
            new OrderMutasiItemDto(
                OrderMutasiId: orderMutasiId,
                NoUrut: 2,
                BrgId: "B002",
                BrgName: "Obat B",
                StokCurrent: 0m,
                Qty: 5m,
                QtyTerpenuhi: 0m,
                SatuanId: "S002",
                SatuanName: "Kotak"
            )
        };
    }

    private static IOrderMutasiKey FakerKey(string orderMutasiId = "OM001")
        => OrderMutasiModel.Key(orderMutasiId);

    [Fact]
    public void InsertBulkTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
        _sut.Delete(FakerKey());

        var items = _sut.ListData(FakerKey()).ToList();
        items.Should().BeEmpty();
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());

        var items = _sut.ListData(FakerKey()).ToList();

        items.Should().HaveCount(2);
        var item1 = items.First(x => x.NoUrut == 1);
        item1.BrgId.Should().Be("B001");
        item1.StokCurrent.Should().Be(25.5m);
        item1.Qty.Should().Be(10m);
        item1.QtyTerpenuhi.Should().Be(3m);
    }

    [Fact]
    public void InsertBulk_Should_Not_Throw_With_Empty_List()
    {
        using var trans = TransHelper.NewScope();
        var action = () => _sut.Insert([]);

        action.Should().NotThrow();
    }
}
