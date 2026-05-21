using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Infrastructure.LabContext.Integration;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabBillingIntegrationTest
{
    [Fact]
    public void CreateTindakan_ReturnsFakeTindakanId()
    {
        var sut = new LabBillingIntegration();

        var tindakanId = sut.CreateTindakan(new LabBillingChargeRequest(
            "LBO000000001",
            "U1",
            [new LabBillingTarifLine("TR1", "T-HB", "Tarif HB")]));

        tindakanId.Should().StartWith("TDK-FAKE-");
    }
}
