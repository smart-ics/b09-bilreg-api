using Ardalis.GuardClauses;
using RestSharp;
using System.Text.Json;

namespace Farinv.Infrastructure.Helpers;

public static class JSendResponse
{
    public static T Read<T>(RestResponse<JSend<T>> response)
    {
        Guard.Against.Null(response, nameof(response));
        if (response.Data is null)
            ReadAndThrowError(response);
        return response.Data!.Data;
    }
    private static void ReadAndThrowError(RestResponseBase response)
    {
        Guard.Against.Null(response, nameof(response));

        if (response.Content is null)
            throw new ArgumentException($"Error Remote: ({(int)response.StatusCode}) {response.ErrorException!.Message}");

        var resultFailed = JsonSerializer.Deserialize<JSend<string>>(response.Content!);
        if (resultFailed != null)
            throw new ArgumentException(resultFailed.Data);
        else
            throw new ArgumentException($"Error Remote: ({(int)response.StatusCode}) {response.ErrorException!.Message}");
    }
}