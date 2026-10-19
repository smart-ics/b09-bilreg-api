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

    [Fact]
    public void ChargeOrder_ReturnsFakeTindakanId()
    {
        var sut = new LabBillingIntegration();
        var order = LabOrderTestSupport.CreateEmrOrder();

        var billingId = sut.ChargeOrder(order, "U1", DateTime.Now);

        billingId.Should().StartWith("TDK-FAKE-");
    }

    [Fact]
    public void CancelCharge_WithParameters_DoesNotThrow()
    {
        var sut = new LabBillingIntegration();

        var act = () => sut.CancelCharge("LBO001", "REG001", "U1", "Cancellation test", DateTime.Now);

        act.Should().NotThrow();
    }

    [Fact]
    public void CancelCharge_WithRequestDto_DoesNotThrow()
    {
        var sut = new LabBillingIntegration();
        var request = new LabBillingCancelRequest("LBO001", "REG001", "U1", "Cancellation test", DateTime.Now);

        var act = () => sut.CancelCharge(request);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateReleaseEligibility_DefaultOrderNo_ReturnsClear()
    {
        var sut = new LabBillingIntegration();
        var result = sut.ValidateReleaseEligibility(
            new LabBillingReleaseValidationRequest("LBO1", "LAB0001", "TDK1"));

        result.Code.Should().Be(BillingReleaseValidationCode.Clear);
    }

    [Fact]
    public void ValidateReleaseEligibility_BilBlockSuffix_ReturnsBlocked()
    {
        var sut = new LabBillingIntegration();
        var result = sut.ValidateReleaseEligibility(
            new LabBillingReleaseValidationRequest("LBO1", "LAB-BILBLOCK", "TDK1"));

        result.Code.Should().Be(BillingReleaseValidationCode.Blocked);
        result.Message.Should().NotBeEmpty();
    }
}
