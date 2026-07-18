using Bilreg.Application.Shared.User;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RestSharp;
using System.Text.Json;

namespace Bilreg.Infrastructure.Shared.User;

public interface IUsmanGetTokenService
{
    Task<string?> Get(string provider);
}
public class UsmanGetTokenService : IUsmanGetTokenService
{
    private readonly UsmanOptions _opt;
    private readonly IMemoryCache _cache;
    public UsmanGetTokenService(IOptions<UsmanOptions> opt,
        IMemoryCache cache)
    {
        _opt = opt.Value;
        _cache = cache;
    }

    public async Task<string?> Get(string provider)
    {
        //var result = _cache.Get($"Token{provider}");

        //if (result is not null)
        //    return result.ToString();

        var endpoint = $"{_opt.BaseUrl}/api/token";
        var client = new RestClient(endpoint);
        var req = new RestRequest()
            .AddHeader("ConsId", _opt.ConsId)
            .AddHeader("SecretKey", _opt.SecretKey);

        var response = await client.ExecuteGetAsync<JSend<string>>(req);

        //var cacheOption = new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(1) };
        //_cache.Set($"Token{provider}", response.Data?.Data, cacheOption);

        var jsonOption = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var datas = response.Content?.DeserializeOrThrow<JSend<string>>($"Parsing failed: {response.Content}", jsonOption);

        var result = datas?.Data;
        
        return result;
        
    }
}
