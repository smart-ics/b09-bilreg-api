using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public class DashboardAddRegService : IDashboardAddRegService
{
    public readonly Emr25Options _opt;

    public DashboardAddRegService(IOptions<Emr25Options> opt)
    {
        _opt = opt.Value;
    }

    public void Execute(AddRegCmd cmd)
    {
        AddReg(cmd).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private async Task AddReg(AddRegCmd req)
    {
        if (_opt.BaseApiUrl.Trim().Length == 0)
            return;
        var endpoint = $"{_opt.BaseApiUrl}/api/Dashboard/addReg";
        var client = new RestClient(endpoint);
        var request = new RestRequest()
            .AddJsonBody(req, "application/json");

        await client.ExecutePostAsync(request);
    }


}


