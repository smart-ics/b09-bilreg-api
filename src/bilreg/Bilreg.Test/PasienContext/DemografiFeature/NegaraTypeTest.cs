using Bilreg.Domain.PasienContext.DemografiFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PasienContext.DemografiFeature;

public class NegaraTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new NegaraType("1", "2", "3", "4", "5");
        sut.NegaraId.Should().Be("1");
        sut.NegaraName.Should().Be("2");
        sut.NegaraEngName.Should().Be("3");
        sut.Alpha3.Should().Be("4");
        sut.NegaraNumeric.Should().Be("5");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = NegaraType.Default;
        sut.NegaraId.Should().Be("-");
        sut.NegaraName.Should().Be("-");
        sut.NegaraEngName.Should().Be("-");
        sut.Alpha3.Should().Be("-");
        sut.NegaraNumeric.Should().Be("-");
    }
}