using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.AdmisiContext.BookingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;

public class JadwalPraktekSendToHfisService : IJadwalPraktekSendToHfisService
{
    private readonly JknOptions _opt;
    private readonly IRestClientFactory _restClient;
    private readonly IJknTokenGetService _jknTokenGetService;
    public JadwalPraktekSendToHfisService(IOptions<JknOptions> opt,
        IRestClientFactory restClient,
        IJknTokenGetService jknTokenGetService)
    {
        _opt = opt.Value;
        _restClient = restClient;
        _jknTokenGetService = jknTokenGetService;
    }

    public IEnumerable<JadwalPraktekSendHfisResponse> Execute(JadwalPraktekSendHfisPayload req)
    {
        if (!_opt.IsSendJadwalToHfis) 
        {
            List<JadwalPraktekSendHfisResponse> resp = new List<JadwalPraktekSendHfisResponse>();
            resp.Add(
                new JadwalPraktekSendHfisResponse
                    ("-", "-", 0, "", "", "", "", "", "", "", "", 0, 0, "", "", "", ""));
            return resp.AsEnumerable();
        }

        // TOKEN
        var token = _jknTokenGetService.Execute();
        if (token is null)
            throw new ArgumentException("Username atau Password GetToken Tidak Sesuai");

        // BUILD request
        var user = _opt.ConsId;
        var client = _restClient.Create(_opt.BaseApiUrl);
        var request = new RestRequest($"/api/JadwalDokter/Update", Method.Post);
        request.AddHeader("x-username", user);
        request.AddHeader("x-token", token);
        request.AddBody(req);

        // EXECUTE
        var response = client.Execute(request);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            List<JadwalPraktekSendHfisResponse> listResp = new List<JadwalPraktekSendHfisResponse>();
            listResp.Add(
                new JadwalPraktekSendHfisResponse
                    ("-", "-", 0, "", "", "", "", "", "", "", "", 0, 0, "", "", "", ""));
            return listResp.AsEnumerable();
        }
        // RETURN
        var jsonOption = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        var result = response.Content.DeserializeOrThrow<JSend<List<JadwalPraktekSendHfisResponse>>>
            ($"Parsing failed: {response.Content}", jsonOption);

        return (IEnumerable<JadwalPraktekSendHfisResponse>)result.Data;

    }
}

