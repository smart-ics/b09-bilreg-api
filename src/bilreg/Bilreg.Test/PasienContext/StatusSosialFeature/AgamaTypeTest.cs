using Bilreg.Domain.PasienContext.StatusSosialFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PasienContext.StatusSosialFeature;

public class AgamaTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new AgamaType("1", "2");
        sut.AgamaId.Should().Be("1");
        sut.AgamaName.Should().Be("2");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = AgamaType.Default;
        sut.AgamaId.Should().Be("-");
        sut.AgamaName.Should().Be("-");
    }
}