using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class TrsBillingDalTest
{
    private const string BillingId = "TBILL-DAL-001";
    private const string RegId = "RG-DAL-001";

    private readonly TrsBillingDal _sut = new(ConnStringHelper.GetTestEnv());

    private static TrsBillingDto SampleDto(
        string billingId = BillingId,
        string regId = RegId,
        decimal subTotal = 100_000m,
        string keterangan = "Ket 1") =>
        new(
            billingId,
            1,
            "2026-06-17",
            "10:00:00",
            "2026-06-17 10:00:00",
            regId,
            "LY001",
            "K01",
            "RC001",
            "PET001",
            subTotal,
            10_000m,
            5_000m,
            2_000m,
            97_000m,
            keterangan,
            "Ket 2",
            "REF001",
            2m,
            "MAIN001",
            "MR001",
            "Pasien A",
            "Layanan A",
            "Kelas A",
            "Rekap A");

    private static ITrsBillingKey BillingKey(string billingId = BillingId) =>
        TrsBillType.Key(billingId);

    [Fact]
    public void GivenNewTrsBillingDto_WhenInsert_ThenRowCanBeRetrieved()
    {
        // Given
        using var trans = TransHelper.NewScope();
        var dto = SampleDto();

        // When
        _sut.Insert(dto);
        var actual = _sut.GetData(BillingKey());

        // Then
        actual.fs_kd_trs.Should().Be(dto.fs_kd_trs);
        actual.fn_sub_total.Should().Be(dto.fn_sub_total);
        actual.fs_keterangan.Should().Be(dto.fs_keterangan);
    }

    [Fact]
    public void GivenExistingRow_WhenUpdate_ThenGetDataReturnsUpdatedValues()
    {
        // Given
        using var trans = TransHelper.NewScope();
        _sut.Insert(SampleDto());
        var updated = SampleDto(subTotal: 200_000m, keterangan: "Updated ket");

        // When
        _sut.Update(updated);
        var actual = _sut.GetData(BillingKey());

        // Then
        actual.fn_sub_total.Should().Be(200_000m);
        actual.fs_keterangan.Should().Be("Updated ket");
        actual.fs_kd_reg.Should().Be(RegId);
    }

    [Fact]
    public void GivenExistingRow_WhenDelete_ThenRowIsNoLongerListed()
    {
        // Given
        using var trans = TransHelper.NewScope();
        _sut.Insert(SampleDto());

        // When
        _sut.Delete(BillingKey());
        var actual = _sut.ListData(RegModel.Key(RegId))?.ToList() ?? [];

        // Then
        actual.Should().NotContain(x => x.fs_kd_trs == BillingId);
    }

    [Fact]
    public void GivenInsertedRow_WhenGetData_ThenPersistedColumnsMatch()
    {
        // Given
        using var trans = TransHelper.NewScope();
        var dto = SampleDto();
        _sut.Insert(dto);

        // When
        var actual = _sut.GetData(BillingKey());

        // Then
        actual.Should().BeEquivalentTo(dto, opt => opt
            .Excluding(x => x.fs_mr)
            .Excluding(x => x.fs_nm_pasien)
            .Excluding(x => x.fs_nm_layanan)
            .Excluding(x => x.fs_nm_kelas)
            .Excluding(x => x.fs_nm_rekap_cetak));
    }

    [Fact]
    public void GivenInsertedRow_WhenListDataByReg_ThenRowIsIncluded()
    {
        // Given
        using var trans = TransHelper.NewScope();
        var dto = SampleDto();
        _sut.Insert(dto);

        // When
        var actual = _sut.ListData(RegModel.Key(RegId))?.ToList() ?? [];

        // Then
        actual.Should().ContainEquivalentOf(dto, opt => opt
            .Excluding(x => x.fs_mr)
            .Excluding(x => x.fs_nm_pasien)
            .Excluding(x => x.fs_nm_layanan)
            .Excluding(x => x.fs_nm_kelas)
            .Excluding(x => x.fs_nm_rekap_cetak));
    }

    [Fact]
    public void GivenDifferentReg_WhenListData_ThenInsertedRowIsNotReturned()
    {
        // Given
        using var trans = TransHelper.NewScope();
        _sut.Insert(SampleDto());

        // When
        var actual = _sut.ListData(RegModel.Key("RG-OTHER"))?.ToList() ?? [];

        // Then
        actual.Should().NotContain(x => x.fs_kd_trs == BillingId);
    }
}
