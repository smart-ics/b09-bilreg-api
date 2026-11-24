using System.Text.Json;
using Bilreg.Application.IgdContext;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Infrastructure.IgdContext.Integration;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Bilreg.Test.IgdContext.Integration;

public class SmassAssessmentGatewayTest : IDisposable
{
    private readonly WireMockServer _server;
    private readonly Mock<ISmassTokenService> _tokenServiceMock;
    private readonly SmassAssessmentGateway _sut;

    private const string LinkPath = "/api/Assesment/linkIgdVisit";
    private const string GeneratePath = "/api/Assesment/generateIgdTriage";

    public SmassAssessmentGatewayTest()
    {
        _server = WireMockServer.Start();

        var smassOptions = Options.Create(new SmassOptions
        {
            BaseApiUrl = _server.Urls[0],
            TokenEmail = "test@example.com",
            TokenPass = "password",
            TimeoutSeconds = 5
        });

        var igdVisitOptions = Options.Create(new IgdVisitOptions
        {
            EnableSmassIntegration = true,
            SmassLayananId = "LAY-IGD",
            SmassTriagePaperId = "PP-TRG"
        });

        _tokenServiceMock = new Mock<ISmassTokenService>();
        _tokenServiceMock
            .Setup(x => x.GetToken(It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-bearer-token");

        var restClientFactory = new RestClientFactory();

        _sut = new SmassAssessmentGateway(
            smassOptions,
            igdVisitOptions,
            restClientFactory,
            _tokenServiceMock.Object);
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }

    [Fact]
    public async Task LinkIgdVisit_WhenSuccessfulWithAssessmentIds_ReturnsSuccessAndMappedAssessmentIds()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            status = "success",
            data = new
            {
                igdVisitId = "VISIT-001",
                linkedCount = 2,
                listAssesmentId = new[] { "ASM-101", "ASM-102" }
            }
        });

        _server
            .Given(Request.Create()
                .WithPath(LinkPath)
                .UsingPatch())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));

        var request = new SmassLinkIgdVisitRequest
        {
            IgdVisitId = "VISIT-001",
            RegId = "REG-001",
            PasienId = "PAS-001",
            PasienName = "John Doe",
            LayananId = "LAY-IGD",
            LayananName = "Instalasi Gawat Darurat"
        };

        // Act
        var result = await _sut.LinkIgdVisit(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AssessmentId.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.LinkedAssessmentIds.Should().NotBeNull();
        result.LinkedAssessmentIds.Should().Equal("ASM-101", "ASM-102");
    }

    [Fact]
    public async Task LinkIgdVisit_WhenSuccessfulWithEmptyAssessmentIds_ReturnsSuccessAndEmptyList()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            status = "success",
            data = new
            {
                igdVisitId = "VISIT-001",
                linkedCount = 0,
                listAssesmentId = Array.Empty<string>()
            }
        });

        _server
            .Given(Request.Create()
                .WithPath(LinkPath)
                .UsingPatch())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));

        var request = new SmassLinkIgdVisitRequest
        {
            IgdVisitId = "VISIT-001",
            RegId = "REG-001"
        };

        // Act
        var result = await _sut.LinkIgdVisit(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.LinkedAssessmentIds.Should().NotBeNull();
        result.LinkedAssessmentIds.Should().BeEmpty();
    }

    [Fact]
    public async Task LinkIgdVisit_WhenSuccessfulWithNullAssessmentIds_ReturnsSuccessAndEmptyList()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            status = "success",
            data = new
            {
                igdVisitId = "VISIT-001",
                linkedCount = 0,
                listAssesmentId = (string[]?)null
            }
        });

        _server
            .Given(Request.Create()
                .WithPath(LinkPath)
                .UsingPatch())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));

        var request = new SmassLinkIgdVisitRequest
        {
            IgdVisitId = "VISIT-001",
            RegId = "REG-001"
        };

        // Act
        var result = await _sut.LinkIgdVisit(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.LinkedAssessmentIds.Should().NotBeNull();
        result.LinkedAssessmentIds.Should().BeEmpty();
    }

    [Fact]
    public async Task LinkIgdVisit_WhenSmassReturnsFailStatus_ReturnsFailureWithNullLinkedAssessmentIds()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            status = "fail",
            code = "400",
            message = "Validation error"
        });

        _server
            .Given(Request.Create()
                .WithPath(LinkPath)
                .UsingPatch())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));

        var request = new SmassLinkIgdVisitRequest
        {
            IgdVisitId = "VISIT-001",
            RegId = "REG-001"
        };

        // Act
        var result = await _sut.LinkIgdVisit(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.LinkedAssessmentIds.Should().BeNull();
        result.ErrorMessage.Should().Contain("SMASS linkIgdVisit mengembalikan status 'fail'");
    }

    [Fact]
    public async Task LinkIgdVisit_WhenHttpError_ReturnsFailureWithNullLinkedAssessmentIds()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath(LinkPath)
                .UsingPatch())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var request = new SmassLinkIgdVisitRequest
        {
            IgdVisitId = "VISIT-001",
            RegId = "REG-001"
        };

        // Act
        var result = await _sut.LinkIgdVisit(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.LinkedAssessmentIds.Should().BeNull();
        result.ErrorMessage.Should().Contain("500");
    }

    [Fact]
    public async Task GenerateIgdTriage_WhenSuccessful_ReturnsAssessmentIdAndNullLinkedAssessmentIds()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            status = "success",
            data = new
            {
                assesmentId = "ASM-999",
                igdVisitId = "VISIT-001",
                noTriage = 1,
                registrationLinkStatus = 0,
                assesmentState = "Draft"
            }
        });

        _server
            .Given(Request.Create()
                .WithPath(GeneratePath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));

        var request = new SmassGenerateIgdTriageRequest
        {
            IgdVisitId = "VISIT-001",
            NoTriage = 1,
            UserrId = "USR-001",
            AssesmentDate = "2026-09-28",
            AssesmentTime = "10:00:00",
            AtsLevel = "ATS1",
            TriageColor = "RED"
        };

        // Act
        var result = await _sut.GenerateIgdTriage(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AssessmentId.Should().Be("ASM-999");
        result.ErrorMessage.Should().BeNull();
        result.LinkedAssessmentIds.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAssesment_WhenHttp200_ReturnsSuccessAndSendsBearerToken()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/api/Assesment/ASM-DEL-01")
                .WithHeader("Authorization", "Bearer test-bearer-token")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { status = "success", data = "Done" })));

        // Act
        var result = await _sut.DeleteAssesment("ASM-DEL-01");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AssessmentId.Should().Be("ASM-DEL-01");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAssesment_WhenHttp204NoContent_ReturnsSuccess()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/api/Assesment/ASM-DEL-204")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(204));

        // Act
        var result = await _sut.DeleteAssesment("ASM-DEL-204");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AssessmentId.Should().Be("ASM-DEL-204");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAssesment_WhenHttp404NotFound_ReturnsIdempotentSuccess()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/api/Assesment/ASM-DEL-404")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithBody("Not Found"));

        // Act
        var result = await _sut.DeleteAssesment("ASM-DEL-404");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AssessmentId.Should().Be("ASM-DEL-404");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAssesment_WhenHttp500_ReturnsFailureWithoutThrowing()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/api/Assesment/ASM-DEL-500")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act
        var result = await _sut.DeleteAssesment("ASM-DEL-500");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.AssessmentId.Should().BeNull();
        result.ErrorMessage.Should().Contain("500");
    }

    [Fact]
    public async Task DeleteAssesment_WhenAssessmentIdIsEmpty_ReturnsFailureWithoutCallingToken()
    {
        // Act
        var result = await _sut.DeleteAssesment("   ");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("AssessmentId");
        _tokenServiceMock.Verify(x => x.GetToken(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAssesment_WhenTokenServiceThrows_IsolatesExceptionIntoFailure()
    {
        // Arrange
        _tokenServiceMock
            .Setup(x => x.GetToken(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Token endpoint unreachable"));

        // Act
        var result = await _sut.DeleteAssesment("ASM-DEL-EX");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Token endpoint unreachable");
    }

    [Fact]
    public async Task DeleteAssesment_WhenBaseApiUrlMissing_ReturnsFailureWithoutCallingToken()
    {
        // Arrange
        var gateway = new SmassAssessmentGateway(
            Options.Create(new SmassOptions { BaseApiUrl = "", TokenEmail = "a", TokenPass = "b" }),
            Options.Create(new IgdVisitOptions()),
            new RestClientFactory(),
            _tokenServiceMock.Object);

        // Act
        var result = await gateway.DeleteAssesment("ASM-DEL-CFG");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("BaseApiUrl");
        _tokenServiceMock.Verify(x => x.GetToken(It.IsAny<CancellationToken>()), Times.Never);
    }
}
