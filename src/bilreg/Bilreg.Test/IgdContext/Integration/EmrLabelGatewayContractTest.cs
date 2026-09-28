using Bilreg.Application.IgdContext;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.IgdContext.Integration;

public class EmrLabelGatewayContractTest
{
    [Fact]
    public void EmrAddSmassLabelRequest_ShouldInitializePropertiesCorrectly()
    {
        // Act
        var request = new EmrAddSmassLabelRequest
        {
            AssesmentId = "ASM-001",
            LayananId = "1GD01",
            PaperId = "PP-ICS-TRGE",
            PaperName = "FORMULIR TRIASE IGD",
            RegId = "REG-2026-001",
            UserrId = "USR-01"
        };

        // Assert
        request.AssesmentId.Should().Be("ASM-001");
        request.LayananId.Should().Be("1GD01");
        request.PaperId.Should().Be("PP-ICS-TRGE");
        request.PaperName.Should().Be("FORMULIR TRIASE IGD");
        request.RegId.Should().Be("REG-2026-001");
        request.UserrId.Should().Be("USR-01");
    }

    [Fact]
    public void EmrAddSmassLabelRequest_DefaultValues_ShouldBeEmptyString()
    {
        // Act
        var request = new EmrAddSmassLabelRequest();

        // Assert
        request.AssesmentId.Should().BeEmpty();
        request.LayananId.Should().BeEmpty();
        request.PaperId.Should().BeEmpty();
        request.PaperName.Should().BeEmpty();
        request.RegId.Should().BeEmpty();
        request.UserrId.Should().BeEmpty();
    }

    [Fact]
    public void EmrLabelGatewayResult_ShouldStoreSuccessAndErrorMessage()
    {
        // Act
        var successResult = new EmrLabelGatewayResult(true, null);
        var failResult = new EmrLabelGatewayResult(false, "Timeout");

        // Assert
        successResult.Success.Should().BeTrue();
        successResult.ErrorMessage.Should().BeNull();

        failResult.Success.Should().BeFalse();
        failResult.ErrorMessage.Should().Be("Timeout");
    }

    [Fact]
    public void Emr20Options_ShouldHaveDefaultSectionNameAndEmptyBaseUrl()
    {
        // Act
        var options = new Emr20Options();

        // Assert
        Emr20Options.SECTION_NAME.Should().Be("Emr20");
        options.BaseApiUrl.Should().BeEmpty();
    }

    [Fact]
    public void IgdVisitOptions_ShouldHaveSmassTriagePaperNameProperty()
    {
        // Act
        var options = new IgdVisitOptions
        {
            SmassTriagePaperName = "FORMULIR TRIASE IGD"
        };

        // Assert
        options.SmassTriagePaperName.Should().Be("FORMULIR TRIASE IGD");
    }
}
