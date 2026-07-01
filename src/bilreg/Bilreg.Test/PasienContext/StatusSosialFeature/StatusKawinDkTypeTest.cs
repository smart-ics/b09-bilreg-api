using Bilreg.Domain.PasienContext.StatusSosialFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PasienContext.StatusSosialFeature;

public class StatusKawinDkTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new StatusKawinDkType("1", "2");
        sut.StatusKawinDkId.Should().Be("1");
        sut.StatusKawinDkName.Should().Be("2");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = StatusKawinDkType.Default;
        sut.StatusKawinDkId.Should().Be("-");
        sut.StatusKawinDkName.Should().Be("-");
    }
}