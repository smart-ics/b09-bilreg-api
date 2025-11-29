using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class OpCasePpaDalTest
{
    private readonly OpCasePpaDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<OpCasePpaDto> FakerList()
        => new List<OpCasePpaDto>
        {
            new OpCasePpaDto(
                OrderOpId: "A",
                NoUrut: 1,
                PpaId: "B",
                PpaName: "C",
                Role: "D",
                AssignDate: new DateTime(2023, 1, 1)
            ),
            new OpCasePpaDto(
                OrderOpId: "A",
                NoUrut: 2,
                PpaId: "E",
                PpaName: "F",
                Role: "G",
                AssignDate: new DateTime(2023, 1, 2)
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
        actual.Should().BeEquivalentTo(FakerList(),
            opt => opt.Excluding(x => x.PpaName));
    }
}