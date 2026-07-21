using Farinv.Application.SalesContext.AntrianFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Farinv.Infrastructure.SalesContext.AntrianFeature;

public class AppendTrackerEvidenceService : IAppendTrackerEvidenceService
{
    private readonly BillingOptions _opt;

    public AppendTrackerEvidenceService(IOptions<BillingOptions> opt)
    {
        _opt = opt.Value;
    }

    public AppendPharmacyEvidenceRequest Execute(AppendPharmacyEvidenceRequest request)
    {
        Task.Run(() => AppendAsync(request)).GetAwaiter().GetResult();
        return request;
    }

    private async Task AppendAsync(AppendPharmacyEvidenceRequest request)
    {
        var endpoint = $"{_opt.BaseApiUrl}/api/PasienTracker/pharmacy/evidence";
        var client = new RestClient(endpoint);
        var req = new RestRequest("", Method.Post)
            .AddJsonBody(new AppendPharmacyEvidenceDto(
                request.PasienTrackerId,
                request.EventName,
                request.ReffId,
                request.OccurredAt));

        var response = await client.ExecuteAsync<JSend<string>>(req);
        if (response.StatusCode != System.Net.HttpStatusCode.OK)
            throw new InvalidOperationException(
                $"Failed to append pharmacy tracker evidence for '{request.PasienTrackerId}'.");
    }

    private record AppendPharmacyEvidenceDto(
        string PasienTrackerId,
        string EventName,
        string ReffId,
        DateTime OccurredAt);
}
