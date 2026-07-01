using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public class BridgeOperasiSaveService : IBridgeOperasiSaveService
{
    private readonly HiDokOptions _opt;

    public BridgeOperasiSaveService(IOptions<HiDokOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Execute(BridgeOperasiCmd req)
    {
        BridgeOpSaveAsync(req).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private async Task BridgeOpSaveAsync(BridgeOperasiCmd req)
    {
        var endpoint = $"{_opt.BaseApiUrl}/apis/jkn-v2/api/bridge/operasi/save";
        var client = new RestClient(endpoint);
        var request = new RestRequest()
            .AddJsonBody(req, "application/json");

        //  EXECUTE
        var response = await client.ExecutePostAsync(request);
    }
}
