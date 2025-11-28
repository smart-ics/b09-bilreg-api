using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.BillContext.KamarOperasiFeature;

public class OrderOpStateHistDalTest
{
    private readonly OrderOpStateHistDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<OrderOpStateHistDto> FakerList()
        => new List<OrderOpStateHistDto>
        {
            new OrderOpStateHistDto(
                OrderOpId: "A",
                NoUrut: 1,
                OrderOpState: 1,
                StateTimestamp: new DateTime(2024, 1, 1, 10, 0, 0)
            ),
            new OrderOpStateHistDto(
                OrderOpId: "A",
                NoUrut: 2,
                OrderOpState: 2,
                StateTimestamp: new DateTime(2024, 1, 1, 11, 0, 0)
            )
        };

    private static IOrderOpKey FakerKey()
        => OrderOpModel.Key("A");

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
