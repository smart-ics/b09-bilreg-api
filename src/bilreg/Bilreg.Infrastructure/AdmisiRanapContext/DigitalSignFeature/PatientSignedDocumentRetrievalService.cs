using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Bilreg.Infrastructure.AdmisiRanapContext.DigitalSignFeature;

public class PatientSignedDocumentRetrievalService : IPatientSignedDocumentRetrievalService
{
    private readonly IOftaGeneralConsentClient _oftaClient;
    private readonly HiDokOptions _hiDokOpt;
    private readonly IRestClientFactory _restClient;
    private readonly ILogger<PatientSignedDocumentRetrievalService> _logger;

    public PatientSignedDocumentRetrievalService(
        IOftaGeneralConsentClient oftaClient,
        IOptions<HiDokOptions> hiDokOpt,
        IRestClientFactory restClient,
        ILogger<PatientSignedDocumentRetrievalService> logger)
    {
        _oftaClient = oftaClient;
        _hiDokOpt = hiDokOpt.Value;
        _restClient = restClient;
        _logger = logger;
    }

    public async Task<byte[]> RetrievePatientSignedPdfAsync(
        string signingRequestId,
        string signedDocUrl,
        CancellationToken cancellationToken = default)
    {
        // 1. Try downloading from signedDocUrl via OFTA client if provided
        if (!string.IsNullOrWhiteSpace(signedDocUrl))
        {
            try
            {
                var bytes = await _oftaClient.DownloadSignedDocAsync(signedDocUrl, cancellationToken);
                if (bytes != null && bytes.Length > 0)
                    return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download signed doc from SignedDocUrl {Url}, falling back to signingRequestId retrieval", signedDocUrl);
            }
        }

        // 2. Try retrieval from HiDok/Middleware server-side endpoint if available
        if (!string.IsNullOrWhiteSpace(_hiDokOpt.BaseApiUrl) && !string.IsNullOrWhiteSpace(signingRequestId))
        {
            try
            {
                var client = _restClient.Create(_hiDokOpt.BaseApiUrl);
                var request = new RestRequest($"/api/signing-requests/{signingRequestId}/signed-document", Method.Get);
                request.Timeout = TimeSpan.FromSeconds(30);
                if (!string.IsNullOrWhiteSpace(_hiDokOpt.ApiKey))
                    request.AddHeader("X-Api-Key", _hiDokOpt.ApiKey);

                var response = await client.ExecuteAsync(request, cancellationToken);
                if (response.IsSuccessful && response.RawBytes != null && response.RawBytes.Length > 0)
                {
                    return response.RawBytes;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download signed document for SigningRequestId {Id} from HiDok/Middleware", signingRequestId);
            }
        }

        throw new InvalidOperationException($"Unable to retrieve completed signed PDF for SigningRequestId '{signingRequestId}' from any configured server-side source.");
    }
}
