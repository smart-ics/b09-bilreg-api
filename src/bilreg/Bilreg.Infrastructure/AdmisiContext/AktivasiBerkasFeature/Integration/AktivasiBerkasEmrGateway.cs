using System.Net;
using System.Text.Json;
using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.Integration;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Bilreg.Infrastructure.AdmisiContext.AktivasiBerkasFeature.Integration;

/// <summary>
/// RestSharp adapter gateway untuk komunikasi antara Bilreg API dan EMR 2.0 API (emr20-api).
/// Mengimplementasikan IAktivasiBerkasEmrGateway dengan timeout 15 detik, fail-closed handling,
/// dan deserialisasi JSON case-insensitive sesuai spesifikasi arsitektur BRM-AKTIVASI-BERKAS.
/// </summary>
public class AktivasiBerkasEmrGateway : IAktivasiBerkasEmrGateway
{
    private const string ListDataRoute = "/api/AktifasiBerkas/ListData";
    private const string GetDataRoute = "/api/AktifasiBerkas/GetData";
    private const string SaveRoute = "/api/AktifasiBerkas/Save";
    private const string UpdateRoute = "/api/AktifasiBerkas/Update";
    private const string FindRegisterRoute = "/api/Reg/Find";
    private const string ListReaktifRoute = "/api/AktifasiBerkas/ListReaktif";
    private const string DeactivateRoute = "/api/AktifasiBerkas/Deactivate";
    private const string CleanUpRoute = "/api/AktifasiBerkas/CleanUp";

    private const int DefaultTimeoutSeconds = 15;

    private static readonly JsonSerializerOptions JsonOption = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Emr20Options _options;
    private readonly IRestClientFactory _restClientFactory;

    public AktivasiBerkasEmrGateway(
        IOptions<Emr20Options> options,
        IRestClientFactory restClientFactory)
    {
        _options = options?.Value ?? new Emr20Options();
        _restClientFactory = restClientFactory;
    }

    public async Task<IEnumerable<AktivasiBerkasListItemDto>> ListData(
        string tglAwal,
        string tglAkhir,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.BaseApiUrl))
                return [];

            var client = _restClientFactory.Create(_options.BaseApiUrl);
            var restRequest = new RestRequest(ListDataRoute, Method.Get);
            restRequest.AddQueryParameter("tglYMD1", tglAwal);
            restRequest.AddQueryParameter("tglYMD2", tglAkhir);
            restRequest.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);

            var response = await client.ExecuteGetAsync(restRequest, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return [];

            if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
                return [];

            var items = JsonSerializer.Deserialize<IEnumerable<AktivasiBerkasListItemDto>>(response.Content, JsonOption);
            return items ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<AktivasiBerkasDetailDto?> GetData(
        string id,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.BaseApiUrl) || string.IsNullOrWhiteSpace(id))
                return null;

            var client = _restClientFactory.Create(_options.BaseApiUrl);
            var restRequest = new RestRequest($"{GetDataRoute}/{Uri.EscapeDataString(id)}", Method.Get);
            restRequest.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);

            var response = await client.ExecuteGetAsync(restRequest, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
                return null;

            return JsonSerializer.Deserialize<AktivasiBerkasDetailDto>(response.Content, JsonOption);
        }
        catch
        {
            return null;
        }
    }

    public async Task<AktivasiBerkasMutationResult> Save(
        AktivasiBerkasSaveDto dto,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.BaseApiUrl))
                return AktivasiBerkasMutationResult.Fail("Konfigurasi Emr20:BaseApiUrl belum diisi.");

            if (dto is null)
                return AktivasiBerkasMutationResult.Fail("Payload aktivasi berkas kosong.");

            if (dto.ListRegister is null || !dto.ListRegister.Any())
                return AktivasiBerkasMutationResult.Fail("Daftar register aktivasi berkas kosong.");

            var client = _restClientFactory.Create(_options.BaseApiUrl);
            var restRequest = new RestRequest(SaveRoute, Method.Post)
                .AddStringBody(JsonSerializer.Serialize(dto, JsonOption), DataFormat.Json);
            restRequest.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);

            var response = await client.ExecutePostAsync(restRequest, ct);

            if (!response.IsSuccessful)
                return AktivasiBerkasMutationResult.Fail(
                    DescribeHttpFailure("Save", response),
                    ResolveErrorStatusCode(response));

            var emrResponse = JsonSerializer.Deserialize<AktivasiBerkasMutationResponse>(
                response.Content ?? "{}", JsonOption);
            var resultingId = emrResponse?.AktifasiBerkasId ?? string.Empty;

            return AktivasiBerkasMutationResult.Ok(resultingId, "Transaksi aktivasi berkas berhasil disimpan.");
        }
        catch (Exception ex)
        {
            return AktivasiBerkasMutationResult.Fail($"EMR Save gagal: {ex.Message}", 502);
        }
    }

    public async Task<AktivasiBerkasMutationResult> Update(
        string id,
        AktivasiBerkasUpdateDto dto,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.BaseApiUrl))
                return AktivasiBerkasMutationResult.Fail("Konfigurasi Emr20:BaseApiUrl belum diisi.");

            if (string.IsNullOrWhiteSpace(id))
                return AktivasiBerkasMutationResult.Fail("ID aktivasi berkas tidak valid.");

            if (dto is null)
                return AktivasiBerkasMutationResult.Fail("Payload aktivasi berkas kosong.");

            if (dto.ListRegister is null || !dto.ListRegister.Any())
                return AktivasiBerkasMutationResult.Fail("Daftar register aktivasi berkas kosong.");

            var client = _restClientFactory.Create(_options.BaseApiUrl);
            var restRequest = new RestRequest(UpdateRoute, Method.Post);
            restRequest.AddQueryParameter("aktifasiID", id);
            restRequest.AddStringBody(JsonSerializer.Serialize(dto, JsonOption), DataFormat.Json);
            restRequest.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);

            var response = await client.ExecutePostAsync(restRequest, ct);

            if (!response.IsSuccessful)
                return AktivasiBerkasMutationResult.Fail(
                    DescribeHttpFailure("Update", response),
                    ResolveErrorStatusCode(response));

            var emrResponse = JsonSerializer.Deserialize<AktivasiBerkasMutationResponse>(
                response.Content ?? "{}", JsonOption);
            var resultingId = !string.IsNullOrWhiteSpace(emrResponse?.AktifasiBerkasId)
                ? emrResponse.AktifasiBerkasId
                : id;

            return AktivasiBerkasMutationResult.Ok(resultingId, "Transaksi aktivasi berkas berhasil diperbarui.");
        }
        catch (Exception ex)
        {
            return AktivasiBerkasMutationResult.Fail($"EMR Update gagal: {ex.Message}", 502);
        }
    }

    public async Task<IEnumerable<AktivasiBerkasRegisterItemDto>> FindRegister(
        string keyword,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.BaseApiUrl))
                return [];

            if (string.IsNullOrWhiteSpace(keyword))
                return [];

            var client = _restClientFactory.Create(_options.BaseApiUrl);
            var restRequest = new RestRequest(FindRegisterRoute, Method.Get);
            restRequest.AddQueryParameter("keyword", keyword);
            restRequest.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);

            var response = await client.ExecuteGetAsync(restRequest, ct);

            if (response.StatusCode == HttpStatusCode.NotFound ||
                (response.StatusCode == HttpStatusCode.BadRequest &&
                 response.Content?.Contains("Data not found", StringComparison.OrdinalIgnoreCase) == true))
            {
                return [];
            }

            if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
                return [];

            var rawItems = JsonSerializer.Deserialize<IEnumerable<EmrRegFindItemRaw>>(response.Content, JsonOption);
            return rawItems?.Select(MapRegisterItem).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<IEnumerable<AktivasiBerkasListReaktifDto>> ListReaktif(
        string? layananId,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.BaseApiUrl))
                return [];

            var client = _restClientFactory.Create(_options.BaseApiUrl);
            var restRequest = new RestRequest(ListReaktifRoute, Method.Get);
            if (!string.IsNullOrWhiteSpace(layananId))
            {
                restRequest.AddQueryParameter("layananID", layananId.Trim());
            }
            restRequest.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);

            var response = await client.ExecuteGetAsync(restRequest, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return [];

            if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
                return [];

            return UnwrapAndDeserializeListReaktif(response.Content);
        }
        catch
        {
            return [];
        }
    }

    public async Task<AktivasiBerkasMutationResult> Deactivate(
        string regId,
        string userrId,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.BaseApiUrl))
                return AktivasiBerkasMutationResult.Fail("Konfigurasi Emr20:BaseApiUrl belum diisi.");

            if (string.IsNullOrWhiteSpace(regId))
                return AktivasiBerkasMutationResult.Fail("RegID tidak valid.");

            var client = _restClientFactory.Create(_options.BaseApiUrl);
            var payload = new EmrDeactivatePayloadRaw
            {
                RegID = regId.Trim(),
                UserrID = userrId?.Trim() ?? string.Empty
            };

            var restRequest = new RestRequest(DeactivateRoute, Method.Post)
                .AddStringBody(JsonSerializer.Serialize(payload, JsonOption), DataFormat.Json);
            restRequest.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);

            var response = await client.ExecutePostAsync(restRequest, ct);

            if (!response.IsSuccessful)
                return AktivasiBerkasMutationResult.Fail(
                    DescribeHttpFailure("Deactivate", response),
                    ResolveErrorStatusCode(response));

            return ParseDeactivateResponse(regId, response.Content);
        }
        catch (Exception ex)
        {
            return AktivasiBerkasMutationResult.Fail($"EMR Deactivate gagal: {ex.Message}", 502);
        }
    }

    public async Task<AktivasiBerkasMutationResult> CleanUp(CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.BaseApiUrl))
                return AktivasiBerkasMutationResult.Fail("Konfigurasi Emr20:BaseApiUrl belum diisi.");

            var client = _restClientFactory.Create(_options.BaseApiUrl);
            var restRequest = new RestRequest(CleanUpRoute, Method.Delete);
            restRequest.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);

            var response = await client.ExecuteAsync(restRequest, ct);

            if (!response.IsSuccessful)
                return AktivasiBerkasMutationResult.Fail(
                    DescribeHttpFailure("CleanUp", response),
                    ResolveErrorStatusCode(response));

            return ParseCleanUpResponse(response.Content);
        }
        catch (Exception ex)
        {
            return AktivasiBerkasMutationResult.Fail($"EMR CleanUp gagal: {ex.Message}", 502);
        }
    }

    private static IEnumerable<AktivasiBerkasListReaktifDto> UnwrapAndDeserializeListReaktif(string content)
    {
        var trimmed = content.Trim();
        using var doc = JsonDocument.Parse(trimmed);
        var root = doc.RootElement;
        var targetElement = root;

        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("data", out var dataProp) ||
                root.TryGetProperty("Data", out dataProp))
            {
                targetElement = dataProp;
            }
            else
            {
                return [];
            }
        }

        if (targetElement.ValueKind != JsonValueKind.Array)
            return [];

        var items = JsonSerializer.Deserialize<IEnumerable<AktivasiBerkasListReaktifDto>>(
            targetElement.GetRawText(), JsonOption);

        return items?.Select(g => g with
        {
            ListBerkas = g.ListBerkas ?? []
        }).ToList() ?? [];
    }

    private static AktivasiBerkasMutationResult ParseDeactivateResponse(string regId, string? content)
    {
        const string defaultDeactivateMessage = "Register telah di Deaktivasi";
        if (string.IsNullOrWhiteSpace(content))
            return AktivasiBerkasMutationResult.Ok(regId, defaultDeactivateMessage);

        var trimmed = content.Trim();
        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("status", out var statusProp) &&
                    statusProp.ValueKind == JsonValueKind.String &&
                    !string.Equals(statusProp.GetString(), "success", StringComparison.OrdinalIgnoreCase))
                {
                    var failDetail = ExtractMessageFromElement(root) ?? "Deaktivasi ditolak oleh server EMR.";
                    return AktivasiBerkasMutationResult.Fail($"EMR Deactivate gagal: {failDetail}");
                }

                var successMessage = ExtractMessageFromElement(root);
                if (string.IsNullOrWhiteSpace(successMessage) ||
                    string.Equals(successMessage, "Register berhasil di nonaktifkan", StringComparison.OrdinalIgnoreCase))
                {
                    successMessage = defaultDeactivateMessage;
                }

                return AktivasiBerkasMutationResult.Ok(regId, successMessage);
            }

            if (root.ValueKind == JsonValueKind.String)
            {
                var msg = root.GetString();
                if (string.IsNullOrWhiteSpace(msg) ||
                    string.Equals(msg, "Register berhasil di nonaktifkan", StringComparison.OrdinalIgnoreCase))
                {
                    msg = defaultDeactivateMessage;
                }
                return AktivasiBerkasMutationResult.Ok(regId, msg);
            }
        }
        catch
        {
            // Fallback ke string trimmed jika bukan JSON
        }

        if (string.Equals(trimmed, "Register berhasil di nonaktifkan", StringComparison.OrdinalIgnoreCase))
            return AktivasiBerkasMutationResult.Ok(regId, defaultDeactivateMessage);

        return AktivasiBerkasMutationResult.Ok(regId, string.IsNullOrWhiteSpace(trimmed) ? defaultDeactivateMessage : trimmed);
    }

    private static AktivasiBerkasMutationResult ParseCleanUpResponse(string? content)
    {
        const string defaultCleanUpMessage = "Seluruh register aktif berhasil dibersihkan";
        if (string.IsNullOrWhiteSpace(content))
            return AktivasiBerkasMutationResult.Ok(string.Empty, defaultCleanUpMessage);

        var trimmed = content.Trim();
        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("status", out var statusProp) &&
                    statusProp.ValueKind == JsonValueKind.String &&
                    !string.Equals(statusProp.GetString(), "success", StringComparison.OrdinalIgnoreCase))
                {
                    var failDetail = ExtractMessageFromElement(root) ?? "CleanUp ditolak oleh server EMR.";
                    return AktivasiBerkasMutationResult.Fail($"EMR CleanUp gagal: {failDetail}");
                }

                var successMessage = ExtractMessageFromElement(root) ?? defaultCleanUpMessage;
                return AktivasiBerkasMutationResult.Ok(string.Empty, successMessage);
            }

            if (root.ValueKind == JsonValueKind.String)
            {
                var msg = root.GetString();
                return AktivasiBerkasMutationResult.Ok(string.Empty, string.IsNullOrWhiteSpace(msg) ? defaultCleanUpMessage : msg);
            }
        }
        catch
        {
            // Fallback ke string trimmed jika bukan JSON
        }

        return AktivasiBerkasMutationResult.Ok(string.Empty, string.IsNullOrWhiteSpace(trimmed) ? defaultCleanUpMessage : trimmed);
    }

    private static string? ExtractMessageFromElement(JsonElement root)
    {
        string[] candidateKeys = ["data", "Data", "message", "Message", "detail", "Detail"];
        foreach (var key in candidateKeys)
        {
            if (root.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                var str = prop.GetString();
                if (!string.IsNullOrWhiteSpace(str))
                    return str.Trim();
            }
        }
        return null;
    }

    private static AktivasiBerkasRegisterItemDto MapRegisterItem(EmrRegFindItemRaw raw)
    {
        var listLayanan = (raw.ListLayanan ?? [])
            .Select(l => new AktivasiBerkasRegisterLayananDto
            {
                LayananId = l.LayananID ?? string.Empty,
                LayananName = l.LayananName ?? string.Empty,
                KamarId = l.KamarID ?? string.Empty,
                KamarName = l.KamarName ?? string.Empty,
                DokterId = l.DokterID ?? string.Empty,
                DokterName = l.DokterName ?? string.Empty,
                SesionPoliId = l.SesionPoliID ?? string.Empty,
                AntrianX1Id = l.AntrianX1ID ?? string.Empty
            })
            .ToList();

        var layananNamesFromList = string.Join(", ",
            listLayanan.Select(l => l.LayananName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct());
        var dokterNamesFromList = string.Join(", ",
            listLayanan.Select(l => l.DokterName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct());

        var layananAkhirId = FirstNonEmpty(raw.LayananAkhirID, raw.LayananID);
        var layananAkhirName = FirstNonEmpty(raw.LayananAkhirName, raw.LayananList, raw.LayananName);

        var primaryLayananId = FirstNonEmpty(
            listLayanan.Select(l => l.LayananId).FirstOrDefault(id => !string.IsNullOrWhiteSpace(id)),
            raw.LayananID,
            layananAkhirId);
        var primaryLayananName = FirstNonEmpty(
            layananNamesFromList,
            raw.LayananName,
            layananAkhirName);

        var kodeMedis = FirstNonEmpty(
            listLayanan.Select(l => l.DokterId).FirstOrDefault(id => !string.IsNullOrWhiteSpace(id)),
            raw.KodeMedis);
        var namaMedis = FirstNonEmpty(dokterNamesFromList, raw.NamaMedis);

        var kodeJenisReg = int.TryParse(raw.JenisReg, out var parsedJenisReg)
            ? parsedJenisReg
            : (raw.KodeJenisReg ?? 0);

        return new AktivasiBerkasRegisterItemDto
        {
            RegId = raw.RegID ?? string.Empty,
            BookingId = raw.BookingID ?? string.Empty,
            TglReg = FirstNonEmpty(raw.TglMasuk, raw.TglReg),
            JamReg = FirstNonEmpty(raw.JamMasuk, raw.JamReg),
            KodeMr = FirstNonEmpty(raw.PasienID, raw.KodeMR),
            NamaPasien = FirstNonEmpty(raw.PasienName, raw.NamaPasien),
            KodeTipeJaminan = FirstNonEmpty(raw.TipeJaminanID, raw.KodeTipeJaminan),
            NamaTipeJaminan = FirstNonEmpty(raw.TipeJaminanName, raw.NamaTipeJaminan),
            TglVoid = raw.TglVoid ?? string.Empty,
            JamVoid = raw.JamVoid ?? string.Empty,
            UserrIdVoid = raw.UserrIDVoid ?? string.Empty,
            TglKeluar = raw.TglKeluar ?? string.Empty,
            JamKeluar = raw.JamKeluar ?? string.Empty,
            Umur = raw.Umur ?? string.Empty,
            JenisReg = FirstNonEmpty(raw.JenisReg, raw.KodeJenisReg?.ToString()),
            LayananId = primaryLayananId,
            LayananName = primaryLayananName,
            LayananList = FirstNonEmpty(raw.LayananAkhirName, raw.LayananList, primaryLayananName),
            LayananAkhirId = layananAkhirId,
            LayananAkhirName = layananAkhirName,
            NoSep = raw.NoSep ?? string.Empty,
            KodeJenisReg = kodeJenisReg,
            KodeMedis = kodeMedis,
            NamaMedis = namaMedis,
            ListLayanan = listLayanan
        };
    }

    private static string FirstNonEmpty(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate.Trim();
        }
        return string.Empty;
    }

    private static int ResolveErrorStatusCode(RestResponse response)
    {
        var code = (int)response.StatusCode;
        return code >= 400 && code < 600 ? code : 502;
    }

    private static string DescribeHttpFailure(string action, RestResponse response)
    {
        var bodyMessage = ExtractEmrErrorMessage(response.Content);
        var detail = FirstNonEmpty(bodyMessage, response.ErrorMessage, response.StatusDescription);
        var code = (int)response.StatusCode;

        if (code > 0)
            return $"EMR {action} gagal: {detail} (HTTP {code})".Trim();

        return $"EMR {action} gagal: {detail}".Trim();
    }

    private static string ExtractEmrErrorMessage(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var trimmed = content.Trim();
        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.String)
                return root.GetString() ?? string.Empty;

            if (root.ValueKind == JsonValueKind.Object)
            {
                string[] candidateKeys = ["Message", "message", "detail", "error", "title", "data"];
                foreach (var key in candidateKeys)
                {
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase) &&
                            prop.Value.ValueKind == JsonValueKind.String)
                        {
                            var val = prop.Value.GetString();
                            if (!string.IsNullOrWhiteSpace(val))
                                return val.Trim();
                        }
                    }
                }
            }
        }
        catch
        {
            // Fallback ke plain text jika content bukan JSON valid
        }

        return trimmed;
    }

    private sealed record AktivasiBerkasMutationResponse
    {
        public string? AktifasiBerkasId { get; init; }
    }

    private sealed record EmrRegFindItemRaw
    {
        public string? RegID { get; init; }
        public string? BookingID { get; init; }
        public string? TglMasuk { get; init; }
        public string? JamMasuk { get; init; }
        public string? TglReg { get; init; }
        public string? JamReg { get; init; }
        public string? Umur { get; init; }
        public string? PasienID { get; init; }
        public string? PasienName { get; init; }
        public string? KodeMR { get; init; }
        public string? NamaPasien { get; init; }
        public string? TipeJaminanID { get; init; }
        public string? TipeJaminanName { get; init; }
        public string? KodeTipeJaminan { get; init; }
        public string? NamaTipeJaminan { get; init; }
        public string? TglVoid { get; init; }
        public string? JamVoid { get; init; }
        public string? UserrIDVoid { get; init; }
        public string? TglKeluar { get; init; }
        public string? JamKeluar { get; init; }
        public string? JenisReg { get; init; }
        public int? KodeJenisReg { get; init; }
        public string? LayananAkhirID { get; init; }
        public string? LayananAkhirName { get; init; }
        public string? LayananID { get; init; }
        public string? LayananName { get; init; }
        public string? LayananList { get; init; }
        public string? NoSep { get; init; }
        public string? KodeMedis { get; init; }
        public string? NamaMedis { get; init; }
        public IEnumerable<EmrRegFindLayananRaw>? ListLayanan { get; init; }
    }

    private sealed record EmrRegFindLayananRaw
    {
        public string? LayananID { get; init; }
        public string? LayananName { get; init; }
        public string? KamarID { get; init; }
        public string? KamarName { get; init; }
        public string? DokterName { get; init; }
        public string? DokterID { get; init; }
        public string? SesionPoliID { get; init; }
        public string? AntrianX1ID { get; init; }
    }

    private sealed record EmrDeactivatePayloadRaw
    {
        public string RegID { get; init; } = string.Empty;
        public string UserrID { get; init; } = string.Empty;
    }
}
