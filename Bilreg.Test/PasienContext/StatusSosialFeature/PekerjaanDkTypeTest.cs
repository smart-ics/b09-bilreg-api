using Bilreg.Domain.PasienContext.StatusSosialFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PasienContext.StatusSosialFeature;

public class PekerjaanDkTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new PekerjaanDkType("1", "2");
        sut.PekerjaanDkId.Should().Be("1");
        sut.PekerjaanDkName.Should().Be("2");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = PekerjaanDkType.Default;
        sut.PekerjaanDkId.Should().Be("-");
        sut.PekerjaanDkName.Should().Be("-");
    }
}