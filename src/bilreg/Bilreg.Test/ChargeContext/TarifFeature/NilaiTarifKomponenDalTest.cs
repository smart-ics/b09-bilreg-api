using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class NilaiTarifKompDalTest
{
    private readonly NilaiTarifKompDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<NilaiTarifKompDto> FakerList()
        => new List<NilaiTarifKompDto>
        {
            new NilaiTarifKompDto(
                NilaiTarifId: "A",
                NoUrut: 1,
                KomponenId: "B",
                Nilai: 50000,
                KomponenName: "C"
            ),
            new NilaiTarifKompDto(
                NilaiTarifId: "A",
                NoUrut: 2,
                KomponenId: "D",
                Nilai: 75000,
                KomponenName: "E"
            )
        };

    private static INilaiTarifKey FakerKey()
        => NilaiTarifType.Key("A");

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
            opt => opt.Excluding(x => x.KomponenName));
    }
}