using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Infrastructure.LabContext.Integration;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabBillingIntegrationTest
{
    private readonly LabBillingIntegration _sut = new();

    [Fact]
    public void CreateTindakan_ReturnsFakeTindakanId()
    {
        var tindakanId = _sut.CreateTindakan(new LabBillingChargeRequest("LBO000000001", "U1"));

        tindakanId.Should().StartWith("TDK-FAKE-");
    }

    [Fact]
    public void ValidateReleaseEligibility_WhenOrderNoContainsBlock_ReturnsBlocked()
    {
        var result = _sut.ValidateReleaseEligibility(
            new LabBillingReleaseValidationRequest("LBO1", "LAB-BLOCK-99", "U1"));

        result.Status.Should().Be(BillingReleaseStatusEnum.Blocked);
        result.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ValidateReleaseEligibility_Default_ReturnsClear()
    {
        var result = _sut.ValidateReleaseEligibility(
            new LabBillingReleaseValidationRequest("LBO1", "LAB00000001", "U1"));

        result.Status.Should().Be(BillingReleaseStatusEnum.Clear);
    }
}
