using Bilreg.Domain.SalesContext.ReturJualFeature;
using Bilreg.Infrastructure.SalesContext.ReturJualFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.SalesContext.ReturJualFeature;

public class ReturJualItemDalTest
{
    private readonly ReturJualItemDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<ReturJualItemDto> FakerList() =>
    [
        new ReturJualItemDto(
            ReturJualId: "RUTST00001",
            ReturJualItemId: "RUTST00001001",
            NoUrut: 1,
            IsVoided: false,
            BrgId: "BRG01",
            QtyJual: 10,
            QtyRetur: 4,
            SatuanId: "TAB",
            HargaJual: 1000,
            HargaRetur: 900,
            TaxPerUnit: 10,
            SubTotalJual: 10000,
            SubTotalRetur: 3600,
            SubTotalTax: 40,
            Total: 3640,
            BrgName: "Obat A",
            SatuanName: "Tablet"),
        new ReturJualItemDto(
            ReturJualId: "RUTST00001",
            ReturJualItemId: "RUTST00001002",
            NoUrut: 2,
            IsVoided: false,
            BrgId: "BRG02",
            QtyJual: 5,
            QtyRetur: 2,
            SatuanId: "TAB",
            HargaJual: 2000,
            HargaRetur: 1800,
            TaxPerUnit: 0,
            SubTotalJual: 10000,
            SubTotalRetur: 3600,
            SubTotalTax: 0,
            Total: 3600,
            BrgName: "Obat B",
            SatuanName: "Tablet")
    ];

    private static IReturJualKey FakerKey() => ReturJualModel.Key("RUTST00001");

    [Fact(Skip = "Requires tb_trs_rjual_umum2 in test database (deploy Bilreg.SqlDb SalesContext scripts)")]
    public void InsertBulkTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
    }

    [Fact(Skip = "Requires tb_trs_rjual_umum2 in test database (deploy Bilreg.SqlDb SalesContext scripts)")]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void Insert_Should_Not_Throw_With_Empty_List()
    {
        using var trans = TransHelper.NewScope();
        var action = () => _sut.Insert([]);

        action.Should().NotThrow();
    }

    [Fact(Skip = "Requires tb_trs_rjual_umum2 in test database (deploy Bilreg.SqlDb SalesContext scripts)")]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = FakerList().ToList();
        _sut.Insert(expected);

        var actual = _sut.ListData(FakerKey()).ToList();

        actual.Should().HaveCount(2);
        actual[0].ReturJualItemId.Should().Be("RUTST00001001");
        actual[0].BrgId.Should().Be("BRG01");
        actual[0].QtyRetur.Should().Be(4);
        actual[1].ReturJualItemId.Should().Be("RUTST00001002");
        actual[1].BrgId.Should().Be("BRG02");
    }
}
