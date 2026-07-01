using Bilreg.Domain.PasienContext.StatusSosialFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PasienContext.StatusSosialFeature;

public class PendidikanDkTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new PendidikanDkType("1", "2");
        sut.PendidikanDkId.Should().Be("1");
        sut.PendidikanDkName.Should().Be("2");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = PendidikanDkType.Default;
        sut.PendidikanDkId.Should().Be("-");
        sut.PendidikanDkName.Should().Be("-");
    }
}