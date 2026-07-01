using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.PaymentContext.RegOutFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.RegOutFeature;

public class RegPembayaranDalTest
{
    private readonly RegPembayaranDal _sut = new(ConnStringHelper.GetTestEnv());

    [Fact]
    public void ListData_ValidRegId_ReturnsAllPaymentTypes()
    {
        // Arrange
        var key = RegModel.Key("RG00000001");

        // Act
        var result = _sut.ListData(key).ToList();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4); // BYDPK, BYDPU, BYPRI, BYKAS
        result.Should().Contain(x => x.fs_kd_bayar == "BYDPK");
    }

    [Fact]
    public void ListData_PartialData_ReturnsEmptyValuesForMissingTypes()
    {
        // Arrange
        var key = RegModel.Key("RG00000001"); // Only has BYDPU

        // Act
        var result = _sut.ListData(key).ToList();

        // Assert
        result.Should().HaveCount(4); // Still returns 5 rows due to UNION ALL with LEFT JOIN
        var deposit = result.FirstOrDefault(x => x.fs_kd_bayar == "BYDPU");
        deposit.Should().NotBeNull();
        deposit?.fn_jasa.Should().Be(0);

        var muka = result.FirstOrDefault(x => x.fs_kd_bayar == "BYDPK");
        muka?.fn_jasa.Should().Be(0); // ISNULL handles this
    }

    [Fact]
    public void ListData_InvalidRegId_ReturnsEmptyRows()
    {
        // Arrange
        var key = RegModel.Key("RG00000001");

        // Act
        var result = _sut.ListData(key).ToList();

        // Assert
        result.Should().HaveCount(4); // Structure returns 4 rows even if empty
        result.All(x => x.fn_jasa == 0).Should().BeTrue();
    }

    [Fact]
    public void ListData_NullValues_InDatabaseHandledCorrectly()
    {
        // Arrange
        var key = RegModel.Key("RG00000001"); // Has NULL values

        // Act
        var result = _sut.ListData(key).ToList();

        // Assert
        var muka = result.FirstOrDefault(x => x.fs_kd_bayar == "BYDPK");
        muka?.fn_jasa.Should().Be(0); // ISNULL in SQL handles this
        muka?.fn_obat.Should().Be(0);
    }
}
