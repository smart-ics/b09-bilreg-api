using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public class BridgeOperasiSaveService
{
    private readonly HidokOptions _opt;

    public BridgeOperasiSaveService(IOptions<HidokOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Execute(BridgeOperasiDto req)
    {
        BridgeOpSaveAsync(req).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private async Task BridgeOpSaveAsync(BridgeOperasiDto req)
    {
        var endpoint = $"{_opt.BaseApiUrl}/api/bridge/operasi/save";
        var client = new RestClient(endpoint);
        var request = new RestRequest()
            .AddJsonBody(req, "application/json");

        //  EXECUTE
        await client.ExecutePostAsync(request);
    }
}
