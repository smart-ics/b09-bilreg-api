using Bilreg.Infrastructure.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.RepoTest;

public class KasirSupervisorAuthDalTest
{
    private readonly KasirSupervisorAuthDal _sut;

    public KasirSupervisorAuthDalTest()
    {
        _sut = new KasirSupervisorAuthDal(ConnStringHelper.GetTestEnv());
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("   ", "password")]
    [InlineData("USER01", "")]
    [InlineData("USER01", "   ")]
    public void ValidateSupervisor_GivenEmptyInputs_ReturnsFalse(string userId, string password)
    {
        var result = _sut.ValidateSupervisor(userId, password);
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSupervisor_GivenInvalidCredentials_ReturnsFalse()
    {
        var result = _sut.ValidateSupervisor("NON_EXISTENT_SUPERVISOR_XYZ", "WRONG_PASS");
        result.Should().BeFalse();
    }
}
