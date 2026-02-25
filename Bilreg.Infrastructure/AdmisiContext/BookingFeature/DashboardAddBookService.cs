using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public class DashboardAddBookService : IDashboardAddBookService
{
    public readonly Emr25Options _opt;

    public DashboardAddBookService(IOptions<Emr25Options> opt)
    {
        _opt = opt.Value;
    }
    public void Execute(AddBookCmd cmd)
    {
        AddBook(cmd).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private async Task AddBook(AddBookCmd req)
    {
        if (_opt.BaseApiUrl.Trim().Length == 0)
            return;
        var endpoint = $"{_opt.Equals}/api/Dashboard/addBooking";
        var client = new RestClient(endpoint);
        var request = new RestRequest()
            .AddJsonBody(req, "application/json");

        await client.ExecutePatchAsync(request);
    }
}
