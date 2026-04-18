using RestSharp;

namespace Bilreg.Infrastructure.Shared.Helpers;

public interface IRestClientFactory
{
    RestClient Create(string endpoint);
}

public class RestClientFactory : IRestClientFactory
{
    public RestClient Create(string endpoint)
    {
        return new RestClient(endpoint);
    }
}
