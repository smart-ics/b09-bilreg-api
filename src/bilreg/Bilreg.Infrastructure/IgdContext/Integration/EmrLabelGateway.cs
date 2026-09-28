using System.Text.Json;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Bilreg.Infrastructure.IgdContext.Integration;

/// <summary>
/// RestSharp adapter for the BILREG to EMR 2.0 outbound label contract (architecture TD-01, AC-02).
/// Calls {Emr20Api} POST /api/LabelV2/AddSmass to register clinical triage labels into EMR 2.0.
/// Enforces fail-closed configuration validation and absorbs all transport/HTTP exceptions
/// to guarantee fault-isolation for registration workflows (AR-05).
/// </summary>
public class EmrLabelGateway : IEmrLabelGateway
{
    private const string AddSmassRoute = "/api/LabelV2/AddSmass";
    private const int DefaultTimeoutSeconds = 10;

    private static readonly JsonSerializerOptions JsonOption = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Emr20Options _options;
    private readonly IRestClientFactory _restClientFactory;

    public EmrLabelGateway(
        IOptions<Emr20Options> options,
        IRestClientFactory restClientFactory)
    {
        _options = options?.Value ?? new Emr20Options();
        _restClientFactory = restClientFactory;
    }

    public async Task<EmrLabelGatewayResult> AddSmassLabel(
        EmrAddSmassLabelRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.BaseApiUrl))
                return new EmrLabelGatewayResult(false, "Konfigurasi Emr20:BaseApiUrl belum diisi.");

            if (request is null)
                return new EmrLabelGatewayResult(false, "Payload AddSmass kosong.");

            var client = _restClientFactory.Create(_options.BaseApiUrl);
            var restRequest = new RestRequest(AddSmassRoute, Method.Post)
                .AddStringBody(JsonSerializer.Serialize(request, JsonOption), DataFormat.Json);
            restRequest.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);

            var response = await client.ExecutePostAsync(restRequest, cancellationToken);
            if (!response.IsSuccessful)
                return new EmrLabelGatewayResult(false, DescribeHttpFailure(response));

            return new EmrLabelGatewayResult(true, null);
        }
        catch (Exception ex)
        {
            return new EmrLabelGatewayResult(false, $"EMR AddSmass gagal: {ex.Message}");
        }
    }

    private static string DescribeHttpFailure(RestResponse response)
    {
        var detail = string.IsNullOrWhiteSpace(response.ErrorMessage)
            ? response.StatusDescription
            : response.ErrorMessage;
        return $"EMR AddSmass gagal: HTTP {(int)response.StatusCode} {detail}";
    }
}
