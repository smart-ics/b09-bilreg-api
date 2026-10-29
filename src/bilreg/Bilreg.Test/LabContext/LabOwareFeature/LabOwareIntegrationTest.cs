using System.Net;
using System.Text;
using Bilreg.Application.LabContext.LabOwareFeature.Integration;
using Bilreg.Infrastructure.LabContext.Integration;
using Bilreg.Infrastructure.LabContext.LabOwareFeature;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bilreg.Test.LabContext.LabOwareFeature;

public class LabOwareIntegrationTest
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Handler { get; set; }

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            Handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Handler(request, cancellationToken);
        }
    }

    private static IOptions<OwareOptions> CreateOptions(
        string baseUrl = "http://localhost:5255",
        string endpoint = "/api/order/lab/bilreg",
        int timeoutSeconds = 10)
    {
        return Options.Create(new OwareOptions
        {
            BaseUrl = baseUrl,
            OrderLabEndpoint = endpoint,
            TimeoutSeconds = timeoutSeconds,
            MaxRetryCount = 3
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Send_WhenPayloadIsEmpty_ReturnsFailureWithoutSending(string? payload)
    {
        // Arrange
        var handlerCalled = false;
        var handler = new FakeHttpMessageHandler((_, _) =>
        {
            handlerCalled = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var httpClient = new HttpClient(handler);
        var sut = new LabOwareIntegration(httpClient, CreateOptions());

        // Act
        var result = sut.Send(payload!);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Payload is empty");
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public void Send_WhenBaseUrlIsNotConfigured_ReturnsFailureWithoutSending()
    {
        // Arrange
        var handlerCalled = false;
        var handler = new FakeHttpMessageHandler((_, _) =>
        {
            handlerCalled = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var httpClient = new HttpClient(handler);
        var sut = new LabOwareIntegration(httpClient, CreateOptions(baseUrl: ""));

        // Act
        var result = sut.Send("{\"orderId\":\"ORD-001\"}");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("BaseUrl is not configured");
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public void Send_When200Ok_WithJSendSuccess_ReturnsSuccess()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var jsonResponse = """
        {
            "status": "success",
            "data": {
                "success": true,
                "orderId": "ORD-001",
                "owareOrderId": "OW-100",
                "vendorReffId": "LIS-999",
                "message": "Order successfully routed to Sysmex"
            }
        }
        """;

        var handler = new FakeHttpMessageHandler(async (req, ct) =>
        {
            capturedRequest = req;
            capturedBody = await req.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(handler);
        var sut = new LabOwareIntegration(httpClient, CreateOptions());
        var payload = "{\"orderId\":\"ORD-001\"}";

        // Act
        var result = sut.Send(payload);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Be("http://localhost:5255/api/order/lab/bilreg");
        capturedRequest.Method.Should().Be(HttpMethod.Post);
        capturedBody.Should().Be(payload);
    }

    [Fact]
    public void Send_When200Ok_WithJSendDataSuccessFalse_ReturnsFailureWithMessage()
    {
        // Arrange
        var jsonResponse = """
        {
            "status": "success",
            "data": {
                "success": false,
                "message": "LIS connection timeout",
                "orderId": "ORD-002"
            }
        }
        """;

        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            }));

        using var httpClient = new HttpClient(handler);
        var sut = new LabOwareIntegration(httpClient, CreateOptions());

        // Act
        var result = sut.Send("{\"orderId\":\"ORD-002\"}");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("LIS connection timeout");
    }

    [Fact]
    public void Send_When200Ok_WithNonJsonResponse_ReturnsSuccess()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("OK", Encoding.UTF8, "text/plain")
            }));

        using var httpClient = new HttpClient(handler);
        var sut = new LabOwareIntegration(httpClient, CreateOptions());

        // Act
        var result = sut.Send("{\"orderId\":\"ORD-003\"}");

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Send_When400BadRequest_ReturnsFailureWithStatusCode()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Invalid orderId parameter", Encoding.UTF8, "text/plain")
            }));

        using var httpClient = new HttpClient(handler);
        var sut = new LabOwareIntegration(httpClient, CreateOptions());

        // Act
        var result = sut.Send("{\"orderId\":\"INVALID\"}");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("400");
        result.ErrorMessage.Should().Contain("Bad Request");
        result.ErrorMessage.Should().Contain("Invalid orderId parameter");
    }

    [Fact]
    public void Send_When500InternalServerError_ReturnsFailureWithStatusCode()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Database connection failed", Encoding.UTF8, "text/plain")
            }));

        using var httpClient = new HttpClient(handler);
        var sut = new LabOwareIntegration(httpClient, CreateOptions());

        // Act
        var result = sut.Send("{\"orderId\":\"ORD-004\"}");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("500");
        result.ErrorMessage.Should().Contain("Internal Server Error");
    }

    [Fact]
    public void Send_WhenTimeoutOccurs_ReturnsFailureWithTimeoutMessage()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler((_, ct) =>
        {
            throw new OperationCanceledException("The operation was canceled.");
        });

        using var httpClient = new HttpClient(handler);
        var sut = new LabOwareIntegration(httpClient, CreateOptions(timeoutSeconds: 5));

        // Act
        var result = sut.Send("{\"orderId\":\"ORD-005\"}");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("timeout");
    }

    [Fact]
    public void Send_WhenHttpRequestExceptionThrown_ReturnsFailureWithConnectionError()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler((_, _) =>
        {
            throw new HttpRequestException("Connection refused by target host.");
        });

        using var httpClient = new HttpClient(handler);
        var sut = new LabOwareIntegration(httpClient, CreateOptions());

        // Act
        var result = sut.Send("{\"orderId\":\"ORD-006\"}");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("connection error");
        result.ErrorMessage.Should().Contain("Connection refused by target host");
    }

    [Fact]
    public void Send_UsingIHttpClientFactory_CreatesClientSuccessfully()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"success\",\"data\":{\"success\":true}}", Encoding.UTF8, "application/json")
            }));
        var httpClient = new HttpClient(handler);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(LabOwareIntegration.HttpClientName))
            .Returns(httpClient);

        var sut = new LabOwareIntegration(factoryMock.Object, CreateOptions());

        // Act
        var result = sut.Send("{\"orderId\":\"ORD-007\"}");

        // Assert
        result.Success.Should().BeTrue();
        factoryMock.Verify(f => f.CreateClient(LabOwareIntegration.HttpClientName), Times.Once);
    }

    [Fact]
    public void Send_WhenErrorIsVeryLong_TruncatesTo200Chars()
    {
        // Arrange
        var veryLongError = new string('E', 500);
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(veryLongError, Encoding.UTF8, "text/plain")
            }));

        using var httpClient = new HttpClient(handler);
        var sut = new LabOwareIntegration(httpClient, CreateOptions());

        // Act
        var result = sut.Send("{\"orderId\":\"ORD-008\"}");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNull();
        result.ErrorMessage!.Length.Should().BeLessThanOrEqualTo(200);
    }
}
