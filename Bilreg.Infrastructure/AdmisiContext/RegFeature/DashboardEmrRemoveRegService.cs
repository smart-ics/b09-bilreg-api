using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public class DashboardEmrRemoveRegService : IDashboardEmrRemoveRegService
{
    public readonly Emr25Options _opt;

    public DashboardEmrRemoveRegService(IOptions<Emr25Options> opt)
    {
        _opt = opt.Value;
    }
    public void Execute(RemoveRegCmd req)
    {
        RemoveReg(req).ConfigureAwait(false).GetAwaiter().GetResult();
    }
    private async Task RemoveReg(RemoveRegCmd req)
    {
        if (_opt.BaseApiUrl.Trim().Length == 0)
            return;
        var endpoint = $"{_opt.BaseApiUrl}/api/Dashboard/removeRegister";
        var client = new RestClient(endpoint);
        var request = new RestRequest()
            .AddJsonBody(req, "application/json");
            //.AddParameter("regID", req.RegId, ParameterType.QueryString);

        var result = await client.ExecutePatchAsync(request);
    }

    
}
