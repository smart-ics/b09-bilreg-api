using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Nuna.Lib.CleanArchHelper;
using RestSharp;
using System.Net;
using System.Text.Json;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public interface IJknTokenGetService : INunaService<string?>
{ }

public class JknTokenGetService : IJknTokenGetService
{
    private readonly JknOptions _opt;
    private readonly IRestClientFactory _restClient;
    public JknTokenGetService(IOptions<JknOptions> opt, 
        IRestClientFactory restClient)
    {
        _opt = opt.Value;
        _restClient = restClient;
    }

    public string? Execute()
    {
        // BUILD request
        var user = _opt.ConsId;
        var pass = _opt.SecretKey;

        var client = _restClient.Create(_opt.BaseApiUrl);
        var request = new RestRequest($"/api/Token", Method.Get);
        request.AddHeader("x-username", user);
        request.AddHeader("x-password", pass);

        // EXECUTE
        var response = client.Execute(request);
        var jsonOption = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var result = JsonSerializer.Deserialize<TokenGetResp>(response.Content ?? string.Empty, jsonOption);
        if (result?.Metadata.Code != 200)
        {
            return null;
        }

        var token = result.Response?.Token;
        return token;
        
    }
}



public record TokenGetResp(MetadataTokenResp Metadata, ResponseToken? Response);
public record MetadataTokenResp(int Code, string Message);
public record ResponseToken(string Token);
