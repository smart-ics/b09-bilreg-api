using Bilreg.Application.PasienContext.DigitalSignFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RestSharp;
using System.Net;
using System.Text.Json;

namespace Bilreg.Infrastructure.PasienContext.DigitalSignFeature;

public class HiDokPatientSignerResolveClient : IHiDokPatientSignerResolveClient
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    private readonly HiDokOptions _opt;
    private readonly IRestClientFactory _restClient;
    private readonly IMemoryCache _cache;

    public HiDokPatientSignerResolveClient(IOptions<HiDokOptions> opt,
        IRestClientFactory restClient,
        IMemoryCache cache)
    {
        _opt = opt.Value;
        _restClient = restClient;
        _cache = cache;
    }

    public HiDokPatientSignerResolveResponse Execute(HiDokPatientSignerResolveRequest req)
    {
        var cacheKey = BuildCacheKey(req);
        if (_cache.TryGetValue(cacheKey, out HiDokSignerResolveData? cached) && cached is not null)
            return ToSuccessResponse(cached);

        var response = ResolveFromHiDok(req);

        if (response.Status == HiDokPatientSignerResolveStatus.Success)
            _cache.Set(cacheKey, new HiDokSignerResolveData(response.UserrId, response.SignerId), CacheTtl);

        return response;
    }

    private HiDokPatientSignerResolveResponse ResolveFromHiDok(HiDokPatientSignerResolveRequest req)
    {
        var client = _restClient.Create(_opt.BaseApiUrl);

        var request = new RestRequest("/api/digital-sign/patient-signers/resolve", Method.Get);
        request.AddQueryParameter("hospitalId", req.HospitalId);
        request.AddQueryParameter("mr", req.NoMr);
        request.AddHeader("X-Api-Key", _opt.ApiKey);

        var response = client.Execute(request);

        var jsonOption = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                var result = response.Content?.DeserializeOrThrow<JSend<HiDokSignerResolveData>>(
                    $"Parsing failed: {response.Content}", jsonOption);
                return new HiDokPatientSignerResolveResponse(
                    HiDokPatientSignerResolveStatus.Success,
                    result?.Data?.UserrId ?? string.Empty,
                    result?.Data?.SignerId ?? string.Empty,
                    string.Empty);

            case HttpStatusCode.NotFound:
                return new HiDokPatientSignerResolveResponse(
                    HiDokPatientSignerResolveStatus.NotFound,
                    string.Empty, string.Empty, "Pasien Belum Terdaftar Di PenaEl");

            case HttpStatusCode.BadGateway:
                return new HiDokPatientSignerResolveResponse(
                    HiDokPatientSignerResolveStatus.ProvisionFailed,
                    string.Empty, string.Empty, "Gagal Mempersiapkan Signer Pasien");

            case HttpStatusCode.Unauthorized:
                return new HiDokPatientSignerResolveResponse(
                    HiDokPatientSignerResolveStatus.Unauthorized,
                    string.Empty, string.Empty, "Kredensial Server Ke HiDok Tidak Valid");

            default:
                return new HiDokPatientSignerResolveResponse(
                    HiDokPatientSignerResolveStatus.Error,
                    string.Empty, string.Empty, $"Gagal Menghubungi HiDok: {response.StatusCode}");
        }
    }

    private static string BuildCacheKey(HiDokPatientSignerResolveRequest req) =>
        $"hidok:patient-signer:{req.HospitalId}:{req.NoMr}";

    private static HiDokPatientSignerResolveResponse ToSuccessResponse(HiDokSignerResolveData data) =>
        new(HiDokPatientSignerResolveStatus.Success,
            data.UserrId, data.SignerId, string.Empty);
}

public record HiDokSignerResolveData(string UserrId, string SignerId);