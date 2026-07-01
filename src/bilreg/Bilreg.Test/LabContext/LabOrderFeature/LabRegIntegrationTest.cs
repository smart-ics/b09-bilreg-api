using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Infrastructure.LabContext.Integration;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabRegIntegrationTest
{
    [Fact]
    public void CreateExecutionRegistration_ReturnsFakeRegId()
    {
        var sut = new LabRegIntegration();

        var regId = sut.CreateExecutionRegistration(new LabRegExecutionRequest("LBO000000001", "U1"));

        regId.Should().StartWith("REG-FAKE-");
    }
}
