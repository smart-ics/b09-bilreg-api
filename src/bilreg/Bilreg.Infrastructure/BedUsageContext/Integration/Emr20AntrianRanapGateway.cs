using Bilreg.Application.BedUsageContext.Integration;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Bilreg.Infrastructure.BedUsageContext.Integration;

/// <summary>
/// RestSharp adapter for EMR 2.0 Inpatient Queue outbound integration (architecture TD-01, TD-02, TD-03).
/// Dispatches async non-blocking HTTP notifications to EMR 2.0 queue endpoints with a 5-second timeout,
/// fail-safe exception absorption, and structured diagnostic logging.
/// </summary>
public class Emr20AntrianRanapGateway : IEmr20AntrianRanapGateway
{
    private const int TimeoutSeconds = 5;

    private readonly Emr20Options _options;
    private readonly IRestClientFactory _restClientFactory;
    private readonly ILogger<Emr20AntrianRanapGateway> _logger;

    public Emr20AntrianRanapGateway(
        IOptions<Emr20Options> options,
        IRestClientFactory restClientFactory,
        ILogger<Emr20AntrianRanapGateway> logger)
    {
        _options = options?.Value ?? new Emr20Options();
        _restClientFactory = restClientFactory;
        _logger = logger;
    }

    public Task AddRegRanapAsync(string regId, CancellationToken cancellationToken = default)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_options.BaseApiUrl) || string.IsNullOrWhiteSpace(regId))
                    return;

                var client = _restClientFactory.Create(_options.BaseApiUrl);
                var request = new RestRequest("/api/Antrian/AddRegRanap", Method.Post)
                    .AddQueryParameter("regID", regId.Trim());
                request.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

                var response = await client.ExecutePostAsync(request);
                if (!response.IsSuccessful)
                {
                    _logger.LogWarning("EMR20 AddRegRanap failed for {RegId}: {StatusCode} {ErrorMessage}",
                        regId, response.StatusCode, response.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EMR20 AddRegRanap exception for {RegId}", regId);
            }
        });

        return Task.CompletedTask;
    }

    public Task AddRanapGabungAsync(string babyRegId, CancellationToken cancellationToken = default)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_options.BaseApiUrl) || string.IsNullOrWhiteSpace(babyRegId))
                    return;

                var client = _restClientFactory.Create(_options.BaseApiUrl);
                var request = new RestRequest("/api/Antrian/AddRanapGabung", Method.Post)
                    .AddQueryParameter("regID", babyRegId.Trim());
                request.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

                var response = await client.ExecutePostAsync(request);
                if (!response.IsSuccessful)
                {
                    _logger.LogWarning("EMR20 AddRanapGabung failed for {BabyRegId}: {StatusCode} {ErrorMessage}",
                        babyRegId, response.StatusCode, response.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EMR20 AddRanapGabung exception for {BabyRegId}", babyRegId);
            }
        });

        return Task.CompletedTask;
    }

    public Task DischargeRegRanapAsync(string regId, bool isGabung = false, CancellationToken cancellationToken = default)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_options.BaseApiUrl) || string.IsNullOrWhiteSpace(regId))
                    return;

                var client = _restClientFactory.Create(_options.BaseApiUrl);
                var request = new RestRequest("/api/Antrian/DischargeRegRanap", Method.Delete)
                    .AddQueryParameter("regID", regId.Trim())
                    .AddQueryParameter("isGabung", isGabung.ToString().ToLowerInvariant());
                request.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

                var response = await client.ExecuteAsync(request);
                if (!response.IsSuccessful)
                {
                    _logger.LogWarning("EMR20 DischargeRegRanap failed for {RegId} (isGabung={IsGabung}): {StatusCode} {ErrorMessage}",
                        regId, isGabung, response.StatusCode, response.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EMR20 DischargeRegRanap exception for {RegId} (isGabung={IsGabung})", regId, isGabung);
            }
        });

        return Task.CompletedTask;
    }
}
