using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using RestSharp;
using System.Net;
using System.Text.Json;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;

public class GrupjaminanMapGetService : IGrupjaminanMapGetService
{
    private readonly JetliOptions _opt;
    private readonly IRestClientFactory _restClient;
    public GrupjaminanMapGetService(IOptions<JetliOptions> opt, 
        IRestClientFactory restClient)
    {
        _opt = opt.Value;
        _restClient = restClient;
    }
    public GrupJaminanMapGetResponse Execute(GrupJaminanMapGetParam req)
    {
        if (req.TipeJaminanId.Trim().Length == 0)
            return new GrupJaminanMapGetResponse("-", "-", "-");

        var client = _restClient.Create(_opt.BaseApiUrl);
        var request = new RestRequest($"/api/GrupJaminan/map", Method.Get);
        request.AddParameter("tipeJaminanId", req.TipeJaminanId, ParameterType.QueryString);

        var response = client.Execute(request);
        if (response.StatusCode != HttpStatusCode.OK)
            return new GrupJaminanMapGetResponse("-", "-", "-");

        var jsonOption = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var result = response.Content.DeserializeOrThrow<JSend<GrupJaminanMapGetResponse>>($"Parsing failed: {response.Content}", jsonOption);
        return result.Data;
    }

}
