using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bilreg.Application.LabContext.LabOwareFeature.Integration;
using Bilreg.Infrastructure.LabContext.LabOwareFeature;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.LabContext.Integration;

public class LabOwareIntegration : ILabOwareIntegration
{
    public const string HttpClientName = "OwareClient";

    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly HttpClient? _directHttpClient;
    private readonly OwareOptions _options;

    public LabOwareIntegration(
        IHttpClientFactory httpClientFactory,
        IOptions<OwareOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    internal LabOwareIntegration(
        HttpClient httpClient,
        IOptions<OwareOptions> options)
    {
        _directHttpClient = httpClient;
        _options = options.Value;
    }

    public LabOwareSendResult Send(string payloadJson)
    {
        return SendAsync(payloadJson).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<LabOwareSendResult> SendAsync(string payloadJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return new LabOwareSendResult(false, "Payload is empty.");
        }

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return new LabOwareSendResult(false, "OWARE BaseUrl is not configured.");
        }

        var client = _directHttpClient ?? _httpClientFactory?.CreateClient(HttpClientName);
        if (client == null)
        {
            return new LabOwareSendResult(false, "HttpClient could not be created.");
        }

        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var endpoint = string.IsNullOrWhiteSpace(_options.OrderLabEndpoint)
            ? "/api/order/lab/bilreg"
            : (_options.OrderLabEndpoint.StartsWith('/') ? _options.OrderLabEndpoint : "/" + _options.OrderLabEndpoint);
        var targetUrl = $"{baseUrl}{endpoint}";

        using var request = new HttpRequestMessage(HttpMethod.Post, targetUrl);
        request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

        try
        {
            var timeoutSec = _options.TimeoutSeconds > 0 ? _options.TimeoutSeconds : 10;
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var response = await client.SendAsync(request, linkedCts.Token);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(linkedCts.Token);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(content);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("data", out var dataProp))
                        {
                            if (dataProp.TryGetProperty("success", out var dataSuccessProp) && !dataSuccessProp.GetBoolean())
                            {
                                var msg = dataProp.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "OWARE returned failure";
                                return new LabOwareSendResult(false, TruncateError(msg ?? "OWARE returned failure"));
                            }
                        }
                        else if (root.TryGetProperty("success", out var rootSuccessProp) && !rootSuccessProp.GetBoolean())
                        {
                            var msg = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "OWARE returned failure";
                            return new LabOwareSendResult(false, TruncateError(msg ?? "OWARE returned failure"));
                        }
                    }
                    catch
                    {
                        // JSON parsing failed but HTTP 200 -> treat as success
                    }
                }
                return new LabOwareSendResult(true, null);
            }

            var errBody = await response.Content.ReadAsStringAsync(linkedCts.Token);
            var errorMsg = $"OWARE error HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {errBody}".Trim();
            return new LabOwareSendResult(false, TruncateError(errorMsg));
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new LabOwareSendResult(false, TruncateError($"OWARE request timeout after {_options.TimeoutSeconds}s: {ex.Message}"));
        }
        catch (HttpRequestException ex)
        {
            return new LabOwareSendResult(false, TruncateError($"OWARE connection error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return new LabOwareSendResult(false, TruncateError($"OWARE unexpected error: {ex.Message}"));
        }
    }

    private static string TruncateError(string error)
    {
        if (string.IsNullOrWhiteSpace(error)) return "OWARE send failed";
        return error.Length > 200 ? error[..200] : error;
    }
}
