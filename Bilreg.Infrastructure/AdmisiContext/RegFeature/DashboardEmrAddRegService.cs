using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public class DashboardEmrAddRegService : IDashboardEMrAddRegService
{
    public readonly Emr25Options _opt;

    public DashboardEmrAddRegService(IOptions<Emr25Options> opt)
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
        var endpoint = $"{_opt.BaseApiUrl}/api/Antrian/AddRegister";
        var client = new RestClient(endpoint);
        var request = new RestRequest()
            .AddParameter("regID", req.RegId, ParameterType.QueryString);

        var result = await client.ExecutePostAsync(request);
    }


}



