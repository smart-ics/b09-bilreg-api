using Farinv.Domain.InventoryContext.MutasiFeature;
using Farinv.Infrastructure.Helpers;
using Farinv.Infrastructure.InventoryContext.MutasiFeature;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Farinv.Test.InventoryContext.MutasiFeature;

public class OrderMutasiItemDalTest
{
    private readonly OrderMutasiItemDal _sut = new(ConnStringHelper.GetTestEnv());

    private IEnumerable<OrderMutasiItemDto> FakerList()
    {
        return new List<OrderMutasiItemDto>
        {
            new OrderMutasiItemDto(
                OrderMutasiId: "OM001",
                NoUrut: 1,
                BrgId: "B001",
                BrgName: "Obat A",
                Qty: 10,
                SatuanId: "S001",
                SatuanName: "Buah"
            ),
            new OrderMutasiItemDto(
                OrderMutasiId: "OM001",
                NoUrut: 2,
                BrgId: "B002",
                BrgName: "Obat B",
                Qty: 5,
                SatuanId: "S002",
                SatuanName: "Kotak"
            )
        };
    }

    private static IOrderMutasiKey FakerKey()
        => OrderMutasiModel.Key("OM001");

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
    }

    [Fact]
    public void InsertBulk_Should_Not_Throw_With_Empty_List()
    {
        using var trans = TransHelper.NewScope();
        var action = () => _sut.Insert([]);

        action.Should().NotThrow();
    }
}