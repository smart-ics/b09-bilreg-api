using Bilreg.Domain.AdmisiContext.RujukanFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiContext.RujukanFeature;

public class CaraMasukDkTypeTest
{
    [Fact]
    public void DatangSendiri_DoesNotRequireRujukan()
    {
        CaraMasukDkType.DatangSendiri.RequiresRujukan.Should().BeFalse();
        new CaraMasukDkType("8", "DATANG SENDIRI").RequiresRujukan.Should().BeFalse();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    public void OtherKnownCaraMasuk_RequireRujukan(string caraMasukDkId)
    {
        new CaraMasukDkType(caraMasukDkId, "ANY").RequiresRujukan.Should().BeTrue();
    }

    [Fact]
    public void WellKnownConstants_RequireRujukanExceptDatangSendiri()
    {
        CaraMasukDkType.RujukanRs.RequiresRujukan.Should().BeTrue();
        CaraMasukDkType.RujukanPuskesmas.RequiresRujukan.Should().BeTrue();
        CaraMasukDkType.RujukanDokter.RequiresRujukan.Should().BeTrue();
    }
}
