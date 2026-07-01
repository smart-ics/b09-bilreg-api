using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.PaymentContext.RegOutFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.PaymentContext.RegOutFeature;

public class RegBiayaDalTest
{
    private readonly RegBiayaDal _sut = new(ConnStringHelper.GetTestEnv());

    private static RegBiayaDto Faker()
        => new RegBiayaDto(
            fs_kd_reg: "RG001",
            fs_kd_trs_bl_admin: "BL001",
            fn_bl_admin: 5000,
            fs_kd_trs_bl_materai: "BL003",
            fn_bl_materai: 1000,
            fs_kd_trs_bl_bulat_jasa: "",
            fn_bl_bulat_jasa: 0,
            fs_kd_trs_bl_bulat_obat: "",
            fn_bl_bulat_obat: 0
        );

    //[Fact]
    //public void ListData_ValidRegId_ReturnsExpectedBiaya()
    //{
    //    // Arrange
    //    var key = RegModel.Key("RG001");
    //    var expected = Faker();

    //    // Act
    //    using var trans = TransHelper.NewScope();
    //    var actual = _sut.ListData(key).ToList();

    //    // Assert
    //    actual.Should().NotBeNull();
    //    actual.Should().ContainSingle(x => x.fs_kd_reg == "RG001");

    //    var result = actual.First();
    //    result.fs_kd_trs_bl_admin.Should().Be(expected.fs_kd_trs_bl_admin);
    //    result.fn_bl_admin.Should().Be(expected.fn_bl_admin);
    //    result.fs_kd_trs_bl_materai.Should().Be(expected.fs_kd_trs_bl_materai);
    //    result.fn_bl_materai.Should().Be(expected.fn_bl_materai);
    //    result.fs_kd_trs_bl_bulat_jasa.Should().Be(expected.fs_kd_trs_bl_bulat_jasa);
    //    result.fn_bl_bulat_jasa.Should().Be(expected.fn_bl_bulat_jasa);
    //    result.fs_kd_trs_bl_bulat_obat.Should().Be(expected.fs_kd_trs_bl_bulat_obat);
    //    result.fn_bl_bulat_obat.Should().Be(expected.fn_bl_bulat_obat);
    //}

    //[Fact]
    //public void ListData_InvalidRegId_ReturnsEmptyCollection()
    //{
    //    // Arrange
    //    var key = RegModel.Key("RG001");

    //    // Act
    //    using var trans = TransHelper.NewScope();
    //    var actual = _sut.ListData(key).ToList();

    //    // Assert
    //    actual.Should().BeEmpty();
    //}

    //[Fact]
    //public void ListData_WithNullValues_HandlesDatabaseNulls()
    //{
    //    // Arrange
    //    var key = RegModel.Key("RG001");

    //    // Act
    //    using var trans = TransHelper.NewScope();
    //    var actual = _sut.ListData(key).ToList();

    //    // Assert
    //    actual.Should().NotBeNull();

    //    // Dapper maps SQL NULL to default(T), so decimals should be 0, strings should be null/empty
    //    var result = actual.FirstOrDefault();
    //    if (result != null)
    //    {
    //        result.fn_bl_admin.Should().BeGreaterOrEqualTo(0);
    //        result.fn_bl_materai.Should().BeGreaterOrEqualTo(0);
    //        result.fn_bl_bulat_jasa.Should().BeGreaterOrEqualTo(0);
    //        result.fn_bl_bulat_obat.Should().BeGreaterOrEqualTo(0);
    //    }
    //}
}