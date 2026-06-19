using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class TrsBilling2DalTest
{
    private const string BillingId = "TBILL-DAL-002";

    private readonly TrsBilling2Dal _sut = new(ConnStringHelper.GetTestEnv());

    private static TaTrsBilling2Dto SampleDto(
        string billingId = BillingId,
        decimal noUrut = 1,
        decimal trsP = 100_000m) =>
        new(
            billingId,
            noUrut,
            "PDP",
            trsP,
            0m,
            "BY001",
            "2026-06-17",
            "10:00:00",
            "KSR001",
            "MED001",
            "DT001",
            "GR001",
            "PPDP001",
            "PDPT001",
            "DISC001",
            "PDPTL001",
            "PERS001",
            "TAX001",
            "RTR001",
            "Nama Detil Tarif",
            "Nama Grup Rek",
            "Nama Peg Kasir",
            "Nama Peg Medis");

    private static ITrsBillingKey BillingKey(string billingId = BillingId) =>
        TrsBillType.Key(billingId);

    [Fact]
    public void GivenNewBill2Dto_WhenInsert_ThenRowCanBeListed()
    {
        // Given
        using var trans = TransHelper.NewScope();
        var dto = SampleDto();

        // When
        _sut.Insert([dto]);
        var actual = _sut.ListData(BillingKey()).ToList();

        // Then
        actual.Should().HaveCount(1);
        actual[0].fs_kd_trs.Should().Be(dto.fs_kd_trs);
        actual[0].fn_trs_p.Should().Be(dto.fn_trs_p);
    }

    [Fact]
    public void GivenMultipleRows_WhenInsert_ThenAllRowsAreListed()
    {
        // Given
        using var trans = TransHelper.NewScope();
        var first = SampleDto(noUrut: 1, trsP: 50_000m);
        var second = SampleDto(noUrut: 2, trsP: 25_000m);

        // When
        _sut.Insert([first, second]);
        var actual = _sut.ListData(BillingKey()).ToList();

        // Then
        actual.Should().HaveCount(2);
        actual.Should().Contain(x => x.fn_no_urut == 1 && x.fn_trs_p == 50_000m);
        actual.Should().Contain(x => x.fn_no_urut == 2 && x.fn_trs_p == 25_000m);
    }

    [Fact]
    public void GivenInsertedRow_WhenListData_ThenPersistedColumnsMatch()
    {
        // Given
        using var trans = TransHelper.NewScope();
        var dto = SampleDto();
        _sut.Insert([dto]);

        // When
        var actual = _sut.ListData(BillingKey()).ToList();

        // Then
        actual.Should().ContainEquivalentOf(dto, opt => opt
            .Excluding(x => x.fs_nm_detil_tarif)
            .Excluding(x => x.fs_nm_grup_rek)
            .Excluding(x => x.fs_nm_peg_medis)
            .Excluding(x => x.fs_nm_peg_kasir));
    }

    [Fact]
    public void GivenInsertedRows_WhenDeleteByBillingKey_ThenListDataIsEmpty()
    {
        // Given
        using var trans = TransHelper.NewScope();
        _sut.Insert([SampleDto(noUrut: 1), SampleDto(noUrut: 2)]);

        // When
        _sut.Delete(BillingKey());
        var actual = _sut.ListData(BillingKey())?.ToList() ?? [];

        // Then
        actual.Should().BeEmpty();
    }

    [Fact]
    public void GivenZeroAmountRow_WhenInsert_ThenZeroValuesArePersisted()
    {
        // Given
        using var trans = TransHelper.NewScope();
        var dto = SampleDto(trsP: 0m) with { fn_trs_n = 0m };

        // When
        _sut.Insert([dto]);
        var actual = _sut.ListData(BillingKey()).Single();

        // Then
        actual.fn_trs_p.Should().Be(0m);
        actual.fn_trs_n.Should().Be(0m);
    }
}
