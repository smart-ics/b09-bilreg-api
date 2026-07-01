using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public class AddAntrianEmrByRegService : IAddAntrianEmrByRegService
{
    private readonly EmrOptions _opt;

    public AddAntrianEmrByRegService(IOptions<EmrOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Execute(AddAntrianEmrByRegCommand cmd)
    {
        AddReg(cmd).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private async Task AddReg(AddAntrianEmrByRegCommand req)
    {
        if (_opt.BaseApiUrl.Trim().Length == 0)
            return;
        var endpoint = $"{_opt.BaseApiUrl}/api/Dashboard/addReg";
        var client = new RestClient(endpoint);
        var request = new RestRequest()
            .AddJsonBody(req, "application/json");
            //.AddParameter("regID", req.RegId, ParameterType.QueryString);

        var result = await client.ExecutePostAsync(request);
    }
}



