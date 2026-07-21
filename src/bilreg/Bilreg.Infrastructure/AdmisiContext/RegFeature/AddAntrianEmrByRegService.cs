using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;
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

    public void Execute(AddAntrianEmrByRegCommand cmd) => Send(cmd);

    public EmrAntrianSendResult Send(AddAntrianEmrByRegCommand cmd)
        => AddReg(cmd).ConfigureAwait(false).GetAwaiter().GetResult();

    private async Task<EmrAntrianSendResult> AddReg(AddAntrianEmrByRegCommand req)
    {
        if (_opt.BaseApiUrl.Trim().Length == 0)
            return new EmrAntrianSendResult(false, "EMR BaseApiUrl empty");

        var endpoint = $"{_opt.BaseApiUrl}/api/Dashboard/addReg";
        var client = new RestClient(endpoint);
        var request = new RestRequest()
            .AddJsonBody(req, "application/json");

        var response = await client.ExecutePostAsync(request);
        if (!response.IsSuccessful)
        {
            var detail = string.IsNullOrWhiteSpace(response.ErrorMessage)
                ? response.StatusDescription
                : response.ErrorMessage;
            return new EmrAntrianSendResult(
                false,
                $"EMR addReg failed: HTTP {(int)response.StatusCode} {detail}");
        }

        return new EmrAntrianSendResult(true, null);
    }
}
