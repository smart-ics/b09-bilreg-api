using Bilreg.Domain.PasienContext.StatusSosialFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PasienContext.StatusSosialFeature;

public class SukuTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new SukuType("1", "2");
        sut.SukuId.Should().Be("1");
        sut.SukuName.Should().Be("2");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = SukuType.Default;
        sut.SukuId.Should().Be("-");
        sut.SukuName.Should().Be("-");
    }
}
