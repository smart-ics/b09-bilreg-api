using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using RestSharp;
using System.Net;
using System.Text.Json;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;

public class GetGrupJaminanJetliService : IGetGrupJaminanJetliService
{
    private readonly JetliOptions _opt;
    private readonly IRestClientFactory _restClient;
    public GetGrupJaminanJetliService(IOptions<JetliOptions> opt, 
        IRestClientFactory restClient)
    {
        _opt = opt.Value;
        _restClient = restClient;
    }
    public GetGrupJaminanJetliResponse Execute(GetGrupJaminanJetliRequest req)
    {
        if (req.TipeJaminanKey.TipeJaminanId.Trim().Length == 0)
            return new GetGrupJaminanJetliResponse(req.TipeJaminanKey, "-", "-");

        var client = _restClient.Create(_opt.BaseApiUrl);
        var request = new RestRequest($"/api/GrupJaminan/map", Method.Get);
        request.AddParameter("tipeJaminanId", req.TipeJaminanKey.TipeJaminanId, ParameterType.QueryString);

        var response = client.Execute(request);
        if (response.StatusCode != HttpStatusCode.OK)
            return new GetGrupJaminanJetliResponse(req.TipeJaminanKey, "-", "-");

        var jsonOption = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var result = response.Content.DeserializeOrThrow<JSend<GetGrupJaminanJetliResponse>>($"Parsing failed: {response.Content}", jsonOption);
        return result.Data;
    }

}
