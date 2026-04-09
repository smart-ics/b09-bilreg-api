using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public class DashboardEmrRemoveBookingService : IDashboardEmrRemoveBookingService
{
    public readonly Emr25Options _opt;

    public DashboardEmrRemoveBookingService(IOptions<Emr25Options> opt)
    {
        _opt = opt.Value;
    }

    public void Execute(RemoveBookingCmd req)
    {
        RemoveBooking(req).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private async Task RemoveBooking(RemoveBookingCmd req)
    {
        if (_opt.BaseApiUrl.Trim().Length == 0)
            return;
        var endpoint = $"{_opt.BaseApiUrl}/api/Dashboard/removeBooking";
        var client = new RestClient(endpoint);
        var request = new RestRequest()
            .AddJsonBody(req, "application/json");
        //.AddParameter("regID", req.RegId, ParameterType.QueryString);

        var result = await client.ExecutePatchAsync(request);
    }
}
