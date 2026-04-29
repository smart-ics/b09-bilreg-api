using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public class AddAntrianEmrByBookingService : IAddAntrianEmrByBookingService
{
    public readonly EmrOptions _opt;

    public AddAntrianEmrByBookingService(IOptions<EmrOptions> opt)
    {
        _opt = opt.Value;
    }
    public void Execute(AddAntrianEmrByBookingCmd cmd)
    {
        AddBook(cmd).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private async Task AddBook(AddAntrianEmrByBookingCmd req)
    {
        if (_opt.BaseApiUrl.Trim().Length == 0)
            return;
        var endpoint = $"{_opt.BaseApiUrl}/api/Dashboard/addBooking";
        var client = new RestClient(endpoint);
        var request = new RestRequest()
            .AddJsonBody(req, "application/json");

        var response = await client.ExecutePostAsync(request);
    }
}
