using Bilreg.Domain.PasienContext.DemografiFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PasienContext.DemografiFeature;

public class KotaTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new KotaType("1", "2");
        sut.KotaId.Should().Be("1");
        sut.KotaName.Should().Be("2");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = KotaType.Default;
        sut.KotaId.Should().Be("-");
        sut.KotaName.Should().Be("-");
    }
}