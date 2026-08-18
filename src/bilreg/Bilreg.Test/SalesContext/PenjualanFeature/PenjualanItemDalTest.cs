using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Infrastructure.SalesContext.PenjualanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.SalesContext.PenjualanFeature;

public class PenjualanItemDalTest
{
    private readonly PenjualanItemDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<PenjualanItemDto> FakerList() =>
    [
        new PenjualanItemDto(
            PenjualanId: "DUTST00001",
            PenjualanItemId: "DUTST00001001",
            NoUrut: 1,
            IsVoided: false,
            BrgId: "BRG01",
            BrgName: "Obat A",
            TipeBarangId: "01",
            IsRacik: false,
            IsKomponen: false,
            RacikId: string.Empty,
            Dosis: 0,
            DosisTxt: string.Empty,
            Qty: 10,
            SatuanId: "TAB",
            SatuanName: "Tablet",
            Harga: 1000,
            Diskon: 0,
            Embalase: 100,
            SubTotal: 10000,
            TaxProsen: 0,
            Tax: 0,
            Fee: 0,
            Total: 10100,
            Bulat: 0,
            NilaiKlaim: 0,
            TipeJaminanId: "00000",
            Etiket: "3x1 tablet",
            Frequency: 3,
            UnitDose: 1,
            Note: "-"),
        new PenjualanItemDto(
            PenjualanId: "DUTST00001",
            PenjualanItemId: "DUTST00001002",
            NoUrut: 2,
            IsVoided: false,
            BrgId: "BRG02",
            BrgName: "Obat B",
            TipeBarangId: "01",
            IsRacik: false,
            IsKomponen: false,
            RacikId: string.Empty,
            Dosis: 0,
            DosisTxt: string.Empty,
            Qty: 5,
            SatuanId: "TAB",
            SatuanName: "Tablet",
            Harga: 2000,
            Diskon: 0,
            Embalase: 0,
            SubTotal: 10000,
            TaxProsen: 0,
            Tax: 0,
            Fee: 0,
            Total: 10000,
            Bulat: 0,
            NilaiKlaim: 0,
            TipeJaminanId: "00000",
            Etiket: "2x1 tablet",
            Frequency: 2,
            UnitDose: 1,
            Note: "-")
    ];

    private static IPenjualanKey FakerKey() => PenjualanModel.Key("DUTST00001");

    [Fact(Skip = "Requires tb_trs_dobill_umum2 in test database (deploy Bilreg.SqlDb SalesContext scripts)")]
    public void InsertBulkTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
    }

    [Fact(Skip = "Requires tb_trs_dobill_umum2 in test database (deploy Bilreg.SqlDb SalesContext scripts)")]
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

    [Fact(Skip = "Requires tb_trs_dobill_umum2 in test database (deploy Bilreg.SqlDb SalesContext scripts)")]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = FakerList().ToList();
        _sut.Insert(expected);

        var actual = _sut.ListData(FakerKey()).ToList();

        actual.Should().HaveCount(2);
        actual[0].PenjualanItemId.Should().Be("DUTST00001001");
        actual[0].BrgId.Should().Be("BRG01");
        actual[0].Qty.Should().Be(10);
        actual[1].PenjualanItemId.Should().Be("DUTST00001002");
        actual[1].BrgId.Should().Be("BRG02");
    }
}
