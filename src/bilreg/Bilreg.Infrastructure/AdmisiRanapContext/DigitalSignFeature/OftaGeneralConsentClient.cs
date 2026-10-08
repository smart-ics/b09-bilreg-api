using System.Net;
using System.Text.Json;
using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Bilreg.Infrastructure.AdmisiRanapContext.DigitalSignFeature;

public class OftaGeneralConsentClient : IOftaGeneralConsentClient
{
    private readonly OftaOptions _opt;
    private readonly IRestClientFactory _restClient;
    private readonly ILogger<OftaGeneralConsentClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public OftaGeneralConsentClient(
        IOptions<OftaOptions> opt,
        IRestClientFactory restClient,
        ILogger<OftaGeneralConsentClient> logger)
    {
        _opt = opt.Value;
        _restClient = restClient;
        _logger = logger;
    }

    public async Task<OftaGeneralConsentIngestClientResponse> IngestAsync(
        OftaGeneralConsentIngestClientRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = _restClient.Create(_opt.BaseApiUrl);
        var restRequest = new RestRequest("/api/GeneralConsent/ingest", Method.Post);
        restRequest.Timeout = TimeSpan.FromSeconds(_opt.TimeoutSeconds > 0 ? _opt.TimeoutSeconds : 30);

        ApplyAuthHeaders(restRequest);

        restRequest.AddFile("File", request.FileBytes, request.FileName, "application/pdf");
        restRequest.AddParameter("RegId", request.RegId);
        restRequest.AddParameter("DokumenId", request.DokumenId);
        restRequest.AddParameter("ExternalDocumentId", request.ExternalDocumentId);
        restRequest.AddParameter("OfficerRef", request.OfficerRef);
        restRequest.AddParameter("SignPositionDesc", request.SignPositionDesc);

        if (!string.IsNullOrWhiteSpace(request.SignTag))
            restRequest.AddParameter("SignTag", request.SignTag);

        if (request.SignPosition.HasValue)
            restRequest.AddParameter("SignPosition", request.SignPosition.Value.ToString());

        if (!string.IsNullOrWhiteSpace(request.DocTypeId))
            restRequest.AddParameter("DocTypeId", request.DocTypeId);

        if (!string.IsNullOrWhiteSpace(request.DocName))
            restRequest.AddParameter("DocName", request.DocName);

        var response = await client.ExecuteAsync(restRequest, cancellationToken);
        if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
        {
            var errorMessage = ExtractErrorMessage(response, "OFTA Ingest failed");
            _logger.LogError("OFTA Ingest error: StatusCode={StatusCode}, Error={Error}", response.StatusCode, errorMessage);
            throw new HttpRequestException($"OFTA Ingest error ({response.StatusCode}): {errorMessage}", null, response.StatusCode);
        }

        try
        {
            var jsend = JsonSerializer.Deserialize<JSendEnvelope<OftaIngestRawData>>(response.Content, JsonOptions);
            var data = jsend?.Data;
            if (data is null)
                throw new InvalidOperationException($"Invalid OFTA Ingest response payload: {response.Content}");

            return new OftaGeneralConsentIngestClientResponse(
                DocId: data.DocId ?? string.Empty,
                DocState: data.DocState.ToString(),
                RequestedDocUrl: data.RequestedDocUrl ?? string.Empty,
                RegId: data.RegId ?? request.RegId,
                DokumenId: data.DokumenId ?? request.DokumenId,
                ExternalDocumentId: data.ExternalDocumentId ?? request.ExternalDocumentId,
                IsExisting: data.IsExisting);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse OFTA Ingest response: {Content}", response.Content);
            throw new InvalidOperationException($"Failed to parse OFTA Ingest response: {ex.Message}", ex);
        }
    }

    public async Task<OftaGeneralConsentExecuteClientResponse> ExecuteAsync(
        OftaGeneralConsentExecuteClientRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = _restClient.Create(_opt.BaseApiUrl);
        var restRequest = new RestRequest("/api/GeneralConsent/execute", Method.Post);
        restRequest.Timeout = TimeSpan.FromSeconds(_opt.TimeoutSeconds > 0 ? _opt.TimeoutSeconds : 30);

        ApplyAuthHeaders(restRequest);

        var payload = new
        {
            docId = request.DocId,
            officerRef = request.OfficerRef,
            passphrase = request.Passphrase,
            otp = request.Otp
        };
        restRequest.AddJsonBody(payload);

        var response = await client.ExecuteAsync(restRequest, cancellationToken);
        if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
        {
            var errorMessage = ExtractErrorMessage(response, "OFTA Execute failed");
            _logger.LogError("OFTA Execute error: StatusCode={StatusCode}, Error={Error}", response.StatusCode, errorMessage);
            throw new HttpRequestException($"OFTA Execute error ({response.StatusCode}): {errorMessage}", null, response.StatusCode);
        }

        try
        {
            var jsend = JsonSerializer.Deserialize<JSendEnvelope<OftaExecuteRawData>>(response.Content, JsonOptions);
            var data = jsend?.Data;
            if (data is null)
                throw new InvalidOperationException($"Invalid OFTA Execute response payload: {response.Content}");

            return new OftaGeneralConsentExecuteClientResponse(
                DocId: data.DocId ?? request.DocId,
                DocState: data.DocState.ToString(),
                SignState: data.SignState.ToString(),
                SignedDocUrl: data.SignedDocUrl ?? string.Empty,
                OfficerEmail: data.OfficerEmail ?? string.Empty,
                OfficerName: data.OfficerName ?? string.Empty,
                SignedDate: data.SignedDate == default ? DateTime.Now : data.SignedDate,
                IsAlreadySigned: data.IsAlreadySigned);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse OFTA Execute response: {Content}", response.Content);
            throw new InvalidOperationException($"Failed to parse OFTA Execute response: {ex.Message}", ex);
        }
    }

    public async Task<byte[]> DownloadSignedDocAsync(
        string signedDocUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(signedDocUrl))
            throw new ArgumentException("SignedDocUrl cannot be empty.", nameof(signedDocUrl));

        string requestUrl;
        RestClient client;

        if (signedDocUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            signedDocUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(signedDocUrl);
            var baseUrl = $"{uri.Scheme}://{uri.Authority}";
            client = _restClient.Create(baseUrl);
            requestUrl = uri.PathAndQuery;
        }
        else
        {
            client = _restClient.Create(_opt.BaseApiUrl);
            requestUrl = signedDocUrl.StartsWith("/") ? signedDocUrl : $"/{signedDocUrl}";
        }

        var restRequest = new RestRequest(requestUrl, Method.Get);
        restRequest.Timeout = TimeSpan.FromSeconds(_opt.TimeoutSeconds > 0 ? _opt.TimeoutSeconds : 30);
        ApplyAuthHeaders(restRequest);

        var response = await client.ExecuteAsync(restRequest, cancellationToken);
        if (!response.IsSuccessful || response.RawBytes == null || response.RawBytes.Length == 0)
        {
            var error = ExtractErrorMessage(response, "Download signed PDF failed");
            _logger.LogError("OFTA Download signed PDF error: StatusCode={StatusCode}, Error={Error}", response.StatusCode, error);
            throw new HttpRequestException($"Download signed PDF error ({response.StatusCode}): {error}", null, response.StatusCode);
        }

        return response.RawBytes;
    }

    private void ApplyAuthHeaders(RestRequest request)
    {
        if (!string.IsNullOrWhiteSpace(_opt.ApiKey))
            request.AddHeader("X-Api-Key", _opt.ApiKey);

        if (!string.IsNullOrWhiteSpace(_opt.ServiceToken))
            request.AddHeader("Authorization", $"Bearer {_opt.ServiceToken}");
    }

    private static string ExtractErrorMessage(RestResponse response, string fallback)
    {
        if (string.IsNullOrWhiteSpace(response.Content))
            return string.IsNullOrWhiteSpace(response.ErrorMessage) ? fallback : response.ErrorMessage;

        try
        {
            using var doc = JsonDocument.Parse(response.Content);
            var root = doc.RootElement;
            if (root.TryGetProperty("message", out var msgProp) && !string.IsNullOrWhiteSpace(msgProp.GetString()))
                return msgProp.GetString()!;
            if (root.TryGetProperty("data", out var dataProp))
            {
                if (dataProp.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(dataProp.GetString()))
                    return dataProp.GetString()!;
                if (dataProp.ValueKind == JsonValueKind.Object && dataProp.TryGetProperty("message", out var nestedMsg))
                    return nestedMsg.GetString() ?? fallback;
            }
        }
        catch
        {
            // Ignore json parse error and return content snippet
        }

        return response.Content.Length > 200 ? response.Content[..200] : response.Content;
    }

    private class JSendEnvelope<T>
    {
        public string? Status { get; set; }
        public object? Code { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    private class OftaIngestRawData
    {
        public string? DocId { get; set; }
        public object? DocState { get; set; }
        public string? RequestedDocUrl { get; set; }
        public string? RegId { get; set; }
        public string? DokumenId { get; set; }
        public string? ExternalDocumentId { get; set; }
        public bool IsExisting { get; set; }
    }

    private class OftaExecuteRawData
    {
        public string? DocId { get; set; }
        public object? DocState { get; set; }
        public object? SignState { get; set; }
        public string? SignedDocUrl { get; set; }
        public string? OfficerEmail { get; set; }
        public string? OfficerName { get; set; }
        public DateTime SignedDate { get; set; }
        public bool IsAlreadySigned { get; set; }
    }
}
