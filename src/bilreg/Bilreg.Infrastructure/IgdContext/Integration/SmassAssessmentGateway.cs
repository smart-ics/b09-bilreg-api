using System.Text.Json;
using Bilreg.Application.IgdContext;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Bilreg.Infrastructure.IgdContext.Integration;

/// <summary>
/// RestSharp adapter for the BILREG → SMASS outbound assessment contract
/// (architecture §5.2, §6.2, §6.3, §10.1). Exactly one HTTP attempt is made per
/// call; every exception is converted to a failed <see cref="SmassGatewayResult"/>
/// so nothing escapes into the triage/registration handler (BR-10).
/// </summary>
public class SmassAssessmentGateway : ISmassAssessmentGateway
{
    private const string GenerateRoute = "/api/Assesment/generateIgdTriage";
    private const string LinkRoute = "/api/Assesment/linkIgdVisit";
    private const int DefaultTimeoutSeconds = 10;
    private const int MaxTimeoutSeconds = 120;
    private const int ErrorBodyLength = 300;

    private static readonly JsonSerializerOptions JsonOption = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly SmassOptions _smass;
    private readonly IgdVisitOptions _igdVisit;
    private readonly IRestClientFactory _restClientFactory;
    private readonly ISmassTokenService _tokenService;

    public SmassAssessmentGateway(
        IOptions<SmassOptions> smassOptions,
        IOptions<IgdVisitOptions> igdVisitOptions,
        IRestClientFactory restClientFactory,
        ISmassTokenService tokenService)
    {
        _smass = smassOptions.Value;
        _igdVisit = igdVisitOptions.Value;
        _restClientFactory = restClientFactory;
        _tokenService = tokenService;
    }

    public async Task<SmassGatewayResult> GenerateIgdTriage(
        SmassGenerateIgdTriageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var configurationError = ValidateConfiguration();
            if (configurationError is not null)
                return Failed(configurationError);

            if (request is null)
                return Failed("Payload generate IGD triage kosong.");

            // Architecture §6.2: PaperId and LayananId travel with the payload but
            // their values are owned by configuration (D-05, D-06/AR-02). The gateway
            // is the single source of truth for those two fields.
            var payload = request with
            {
                PaperId = _igdVisit.SmassTriagePaperId,
                LayananId = _igdVisit.SmassLayananId
            };

            var token = await _tokenService.GetToken(cancellationToken);
            if (string.IsNullOrWhiteSpace(token))
                return Failed("Token SMASS tidak tersedia.");

            var client = _restClientFactory.Create(_smass.BaseApiUrl);
            var restRequest = new RestRequest(GenerateRoute, Method.Post)
                .AddHeader("Authorization", $"Bearer {token}")
                .AddStringBody(JsonSerializer.Serialize(payload, JsonOption), DataFormat.Json);
            restRequest.Timeout = RequestTimeout;

            var response = await client.ExecutePostAsync(restRequest, cancellationToken);
            if (!response.IsSuccessful)
                return Failed(DescribeHttpFailure("generateIgdTriage", response));

            var envelope = response.Content?.DeserializeOrThrow<JSend<GenerateResponse>>(
                $"Parsing failed: {response.Content}", JsonOption);

            if (envelope is null || !IsSuccess(envelope.Status) || envelope.Data is null)
                return Failed(DescribeEnvelopeFailure(
                    "generateIgdTriage", envelope?.Status, envelope?.Code, response.Content));

            return new SmassGatewayResult(true, envelope.Data.AssesmentId, null);
        }
        catch (Exception ex)
        {
            return Failed($"SMASS generateIgdTriage gagal: {ex.Message}");
        }
    }

    public async Task<SmassGatewayResult> LinkIgdVisit(
        SmassLinkIgdVisitRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var configurationError = ValidateConfiguration();
            if (configurationError is not null)
                return Failed(configurationError);

            if (request is null)
                return Failed("Payload link IGD visit kosong.");

            var token = await _tokenService.GetToken(cancellationToken);
            if (string.IsNullOrWhiteSpace(token))
                return Failed("Token SMASS tidak tersedia.");

            var client = _restClientFactory.Create(_smass.BaseApiUrl);
            var restRequest = new RestRequest(LinkRoute, Method.Patch)
                .AddHeader("Authorization", $"Bearer {token}")
                .AddStringBody(JsonSerializer.Serialize(request, JsonOption), DataFormat.Json);
            restRequest.Timeout = RequestTimeout;

            var response = await client.ExecutePatchAsync(restRequest, cancellationToken);
            if (!response.IsSuccessful)
                return Failed(DescribeHttpFailure("linkIgdVisit", response));

            var envelope = response.Content?.DeserializeOrThrow<JSend<LinkResponse>>(
                $"Parsing failed: {response.Content}", JsonOption);

            if (envelope is null || !IsSuccess(envelope.Status) || envelope.Data is null)
                return Failed(DescribeEnvelopeFailure(
                    "linkIgdVisit", envelope?.Status, envelope?.Code, response.Content));

            // A link operation is visit-level and carries no single AssessmentId (IR-06).
            return new SmassGatewayResult(true, null, null);
        }
        catch (Exception ex)
        {
            return Failed($"SMASS linkIgdVisit gagal: {ex.Message}");
        }
    }

    private TimeSpan RequestTimeout
    {
        get
        {
            var seconds = _smass.TimeoutSeconds is > 0 and <= MaxTimeoutSeconds
                ? _smass.TimeoutSeconds
                : DefaultTimeoutSeconds;
            return TimeSpan.FromSeconds(seconds);
        }
    }

    /// <summary>
    /// Fail-closed configuration validation (AR-02, P-08): any missing value returns
    /// a failed result and no HTTP call is attempted. Architecture §6.3 defers the
    /// link error handling to §6.2, so the same required set is enforced for both
    /// operations.
    /// </summary>
    private string? ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_smass.BaseApiUrl))
            return "Konfigurasi Smass:BaseApiUrl belum diisi.";
        if (string.IsNullOrWhiteSpace(_smass.TokenEmail))
            return "Konfigurasi Smass:TokenEmail belum diisi.";
        if (string.IsNullOrWhiteSpace(_smass.TokenPass))
            return "Konfigurasi Smass:TokenPass belum diisi.";
        if (string.IsNullOrWhiteSpace(_igdVisit.SmassLayananId))
            return "Konfigurasi IgdVisit:SmassLayananId belum diisi.";
        if (string.IsNullOrWhiteSpace(_igdVisit.SmassTriagePaperId))
            return "Konfigurasi IgdVisit:SmassTriagePaperId belum diisi.";

        return null;
    }

    private static bool IsSuccess(string? status)
        => string.Equals(status, "success", StringComparison.OrdinalIgnoreCase);

    private static SmassGatewayResult Failed(string message)
        => new(false, null, message);

    private static string DescribeHttpFailure(string operation, RestResponse response)
    {
        var detail = string.IsNullOrWhiteSpace(response.ErrorMessage)
            ? response.StatusDescription
            : response.ErrorMessage;
        return $"SMASS {operation} gagal: HTTP {(int)response.StatusCode} {detail}";
    }

    private static string DescribeEnvelopeFailure(
        string operation,
        string? status,
        string? code,
        string? content)
    {
        var body = content is { Length: > ErrorBodyLength }
            ? content[..ErrorBodyLength]
            : content;
        return $"SMASS {operation} mengembalikan status '{status ?? "-"}' " +
               $"code '{code ?? "-"}': {body}";
    }

    private sealed class GenerateResponse
    {
        public string AssesmentId { get; set; } = string.Empty;
        public string IgdVisitId { get; set; } = string.Empty;
        public int NoTriage { get; set; }
        public int RegistrationLinkStatus { get; set; }
        public string AssesmentState { get; set; } = string.Empty;
    }

    private sealed class LinkResponse
    {
        public string IgdVisitId { get; set; } = string.Empty;
        public int LinkedCount { get; set; }
        public string[] ListAssesmentId { get; set; } = Array.Empty<string>();
    }
}
