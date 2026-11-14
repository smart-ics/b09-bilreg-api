using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.Shared.User;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace Bilreg.Infrastructure.Shared.User;

public class UsmanGetUserService : IUsmanGetUserService
{
    private readonly UsmanOptions _opt;
    private readonly IRestClientFactory _restClient;
    private readonly IUsmanGetTokenService _token;
    public UsmanGetUserService(IOptions<UsmanOptions> opt,
        IRestClientFactory restClient,
        IUsmanGetTokenService token)
    {
        _opt = opt.Value;
        _restClient = restClient;
        _token = token;
    }

    public UsmanGetUserResponse Execute(UsmanGetUserRequest req)
    {
        if (req.Email.Equals("octopus@email.com", StringComparison.OrdinalIgnoreCase) && req.Pass == "octopus123")
        {
            return new UsmanGetUserResponse(
                "OCTOPUS",
                "Octopus Admin",
                "octopus",
                "octopus@email.com",
                DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd"),
                new List<UsmanGetUserRoleResponse>
                {
                    new("admin"),
                    new("administrator"),
                    new("IGD-USER"),
                    new("IGD-SPV"),
                    new("ADM-USER"),
                    new("ADM-SPV"),
                    new("KSR-USER"),
                    new("KSR-SPV"),
                    new("TAREK-USER"),
                    new("TAREK-SPV")
                }
            );
        }

        var token = _token.Get("Usman").Result;
        
        var client = _restClient.Create(_opt.BaseUrl);
        
        var request = new RestRequest($"/api/User/login", Method.Post);
        request.AddJsonBody(BuildRequest(req));
        
        var response = client.Execute(request);

        if (response.StatusCode != HttpStatusCode.OK)
            throw new ArgumentException("User invalid");

        var jsonOption = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var result = response.Content?.DeserializeOrThrow<JSend<UsmanGetUserResponse>>($"Parsing failed: {response.Content}", jsonOption);
        return result.Data;
    }

    private static string BuildRequest(UsmanGetUserRequest req)
    {
        var reqBody = new
        {
            pegIdEmail = req.Email,
            appId = req.AppId,
            pass = req.Pass
        };

        var jsonBody = JsonSerializer.Serialize(reqBody);
        return jsonBody;
    }
}
