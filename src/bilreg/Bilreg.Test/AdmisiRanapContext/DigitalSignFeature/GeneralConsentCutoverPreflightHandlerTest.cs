using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature.UseCases;
using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.DigitalSignFeature;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.DigitalSignFeature;

public class GeneralConsentCutoverPreflightHandlerTest
{
    private readonly Mock<IOftaGeneralConsentClient> _oftaClientMock = new();

    private GeneralConsentCutoverPreflightHandler CreateHandler(OftaOptions options)
    {
        return new GeneralConsentCutoverPreflightHandler(_oftaClientMock.Object, Options.Create(options));
    }

    [Fact]
    public async Task GivenCompleteConfiguration_WhenHandle_ThenReportsAllPreflightsPassed()
    {
        var options = new OftaOptions
        {
            BaseApiUrl = "http://ofta.hospital.internal:5000",
            ApiKey = "ofta-secret-key-123",
            TimeoutSeconds = 30
        };

        var handler = CreateHandler(options);
        var qry = new GeneralConsentCutoverPreflightQry(
            RequiredDocTypeId: "GENERAL_CONSENT",
            OfficerRefsToCheck: new[] { "officer1@hospital.com", "officer2@hospital.com" });

        var response = await handler.Handle(qry, CancellationToken.None);

        response.Should().NotBeNull();
        response.FeatureEnabled.Should().BeTrue();
        response.AllPreflightsPassed.Should().BeTrue();
        response.DocTypeStatus.IsNonPrint.Should().BeTrue();
        response.DocTypeStatus.IsConfigured.Should().BeTrue();
        response.OfficerStatuses.Should().HaveCount(2);
        response.OfficerStatuses.Should().OnlyContain(x => x.AccountExists && x.IsTteRegistered);
        response.GuardrailsSummary.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GivenMissingOftaConfiguration_WhenHandle_ThenReportsPreflightsFailed()
    {
        var options = new OftaOptions
        {
            BaseApiUrl = "",
            ApiKey = "",
            TimeoutSeconds = 30
        };

        var handler = CreateHandler(options);
        var qry = new GeneralConsentCutoverPreflightQry();

        var response = await handler.Handle(qry, CancellationToken.None);

        response.Should().NotBeNull();
        response.FeatureEnabled.Should().BeFalse();
        response.AllPreflightsPassed.Should().BeFalse();
        response.DocTypeStatus.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task GivenInvalidOfficerReference_WhenHandle_ThenReportsOfficerPrerequisiteFailed()
    {
        var options = new OftaOptions
        {
            BaseApiUrl = "http://ofta.hospital.internal:5000",
            ApiKey = "key",
            TimeoutSeconds = 30
        };

        var handler = CreateHandler(options);
        var qry = new GeneralConsentCutoverPreflightQry(
            OfficerRefsToCheck: new[] { "ab" }); // less than 3 chars and no @

        var response = await handler.Handle(qry, CancellationToken.None);

        response.AllPreflightsPassed.Should().BeFalse();
        response.OfficerStatuses.First().AccountExists.Should().BeFalse();
    }
}
