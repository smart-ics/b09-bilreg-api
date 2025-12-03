using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class OpCaseStateHistDalTest
{
    private readonly OpCaseStateHistDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<OpCaseStateHistDto> FakerList()
        => new List<OpCaseStateHistDto>
        {
            new OpCaseStateHistDto(
                OrderOpId: "A",
                NoUrut: 1,
                OpCaseState: 1,
                StateTimestamp: new DateTime(2023, 1, 1)
            ),
            new OpCaseStateHistDto(
                OrderOpId: "A",
                NoUrut: 2,
                OpCaseState: 2,
                StateTimestamp: new DateTime(2023, 1, 2)
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