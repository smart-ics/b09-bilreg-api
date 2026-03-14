using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.PaymentContext.RegOutFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Test.PaymentContext.RegOutFeature;

public class RegHutangDalTest
{
    private readonly RegHutangDal _sut = new(ConnStringHelper.GetTestEnv());

    private static RegHutangDto CreateExpectedDto(
        string regId,
        string tglPiutang,
        decimal piutang,
        decimal sisa,
        decimal nilaiJasa,
        decimal nilaiObat)
        => new RegHutangDto(
            fs_kd_reg: regId,
            fd_tgl_piutang: tglPiutang,
            fn_piutang: piutang,
            fn_sisa: sisa,
            fn_nilai_jasa: nilaiJasa,
            fn_nilai_obat: nilaiObat
        );

    // FIX #2: Simple IPasienKey implementation for testing
    private static IPasienKey CreateTestKey(string pasienId)
        => PasienModel.Key(pasienId);

    //[Fact]
    //public void ListData_ValidPasienId_ReturnsExpectedHutang()
    //{
    //    // Arrange
    //    var key = CreateTestKey("MR001");
    //    var expected = CreateExpectedDto("RG001", "2026-02-27", 50000, 50000, 30000, 20000);

    //    // Act
    //    using var trans = TransHelper.NewScope();
    //    var actual = _sut.ListData(key).ToList() ?? Enumerable.Empty<RegHutangDto>();

    //    // Assert
    //    actual.Should().NotBeNull();
    //    actual.Should().ContainSingle(x => x.fs_kd_reg == "RG001");

    //    var result = actual.First();
    //    result.fs_kd_reg.Should().Be(expected.fs_kd_reg);
    //    result.fd_tgl_piutang.Should().Be(expected.fd_tgl_piutang);
    //    result.fn_piutang.Should().Be(expected.fn_piutang);
    //    result.fn_sisa.Should().Be(expected.fn_sisa);
    //    result.fn_nilai_jasa.Should().Be(expected.fn_nilai_jasa);
    //    result.fn_nilai_obat.Should().Be(expected.fn_nilai_obat);
    //}

    //[Fact]
    //public void ListData_InvalidPasienId_ReturnsEmptyCollection()
    //{
    //    // Arrange
    //    var key = CreateTestKey("INVALID_PASIEN_999");

    //    // Act
    //    using var trans = TransHelper.NewScope();
    //    var actual = _sut.ListData(key).ToList() ?? Enumerable.Empty<RegHutangDto>();

    //    // Assert
    //    actual.Should().BeEmpty();
    //}

    //[Fact]
    //public void ListData_WithDateFilter_OnlyReturnsValidPiutang()
    //{
    //    // Arrange
    //    var key = CreateTestKey("MR001");
    //    var today = DateTime.Now.ToString(DateFormatEnum.YMD);
    //    var todayParsed = DateTime.Parse(today);

    //    // Act
    //    using var trans = TransHelper.NewScope();
    //    var actual = _sut.ListData(key).ToList() ?? Enumerable.Empty<RegHutangDto>();

    //    // Assert
    //    actual.Should().NotBeNull();

    //    foreach (var item in actual)
    //    {
    //        // FIX: Use regular code block instead of expression tree
    //        if (DateTime.TryParse(item.fd_tgl_piutang, out DateTime tgl))
    //        {
    //            tgl.Should().BeOnOrBefore(todayParsed);
    //        }
    //        item.fn_sisa.Should().BeGreaterThan(0);
    //    }
    //}

    //[Fact]
    //public void ListData_WithVoidedRecords_ExcludesVoidedPiutang()
    //{
    //    // Arrange
    //    var key = CreateTestKey("MR001");

    //    // Act
    //    using var trans = TransHelper.NewScope();
    //    var actual = _sut.ListData(key).ToList() ?? Enumerable.Empty<RegHutangDto>();

    //    // Assert
    //    // fd_tgl_void is filtered in SQL but not returned in DTO.
    //    // Verify indirectly: all returned records must have fn_sisa > 0
    //    // (voided records typically have fn_sisa = 0 or are excluded by other filters)
    //    foreach (var item in actual)
    //    {
    //        item.fn_sisa.Should().BeGreaterThan(0,
    //            "only outstanding (non-voided) piutang should be returned");
    //    }
    //}
}