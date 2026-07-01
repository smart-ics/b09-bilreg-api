using Bilreg.Domain.PasienContext.DemografiFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PasienContext.DemografiFeature;

public class PropinsiTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new PropinsiType("1", "2");
        sut.PropinsiId.Should().Be("1");
        sut.PropinsiName.Should().Be("2");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = PropinsiType.Default;
        sut.PropinsiId.Should().Be("-");
        sut.PropinsiName.Should().Be("-");
    }
}