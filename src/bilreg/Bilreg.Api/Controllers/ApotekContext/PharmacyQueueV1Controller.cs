using System.Text.Json;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Application.Shared;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Api.Controllers.ApotekContext;

#region Request DTOs

/// <summary>
/// HTTP Request body untuk endpoint intake tiket kiosk mandiri antrean farmasi.
/// </summary>
public record PharmacyQueueIntakeRequest(
    string? ServicePointId = null,
    string? DepoFarmasiId = null,
    string? UserId = null,
    string? RegId = null);

/// <summary>
/// HTTP Request body untuk pembaruan daftar titik layanan (Service Point) yang dilayani loket fisik.
/// </summary>
public record PharmacyQueueUpdateLoketServicePointsRequest(
    IReadOnlyList<string>? ServicePointIds = null,
    object? RowVersion = null);

/// <summary>
/// HTTP Request body untuk operasi pemanggilan loket fisik (Call Next dan Call Serah).
/// </summary>
public record PharmacyQueueCallRequest(
    string LoketKey,
    string? AntrianId = null,
    int? NoUrut = null,
    object? ExpectedRowVersion = null);

/// <summary>
/// HTTP Request body untuk pemanggilan ulang (Recall) nomor antrean aktif di loket fisik.
/// </summary>
public record PharmacyQueueRecallRequest(
    string LoketKey,
    object? RowVersion = null);

/// <summary>
/// HTTP Request body untuk melewati (Skip) antrean aktif di loket fisik.
/// </summary>
public record PharmacyQueueSkipRequest(
    string LoketKey,
    string Reason,
    object? RowVersion = null);

/// <summary>
/// HTTP Request body untuk memulai pelayanan tatap muka di loket fisik (Start Service).
/// </summary>
public record PharmacyQueueStartServiceRequest(
    string LoketKey,
    object? RowVersion = null);

/// <summary>
/// HTTP Request body untuk melepaskan klaim loket fisik kembali ke status Idle (Release).
/// </summary>
public record PharmacyQueueReleaseRequest(
    string LoketKey,
    object? RowVersion = null);

/// <summary>
/// HTTP Request body untuk mengaitkan tiket antrean ke e-resep aktif pasien (Demand Mapping).
/// </summary>
public record PharmacyQueueMapDemandRequest(
    string AntrianId,
    int NoUrut,
    QueueDemandKindEnum DemandKind = QueueDemandKindEnum.Resep,
    string DemandId = "",
    IReadOnlyList<string>? DemandIds = null,
    string PasienTrackerId = "",
    QueueMappingMethodEnum MappingMethod = QueueMappingMethodEnum.Manual);

/// <summary>
/// HTTP Request body untuk menandai kehadiran fisik pasien di depan loket penerimaan.
/// </summary>
public record PharmacyQueueFlagPasienHadirRequest(
    string AntrianId,
    int NoUrut,
    QueueDemandKindEnum DemandKind = QueueDemandKindEnum.Resep,
    string DemandId = "");

/// <summary>
/// HTTP Request body untuk penyelesaian penyerahan fisik obat di Loket 2 (Complete Handover).
/// </summary>
public record PharmacyQueueCompleteHandoverRequest(
    string AntrianId,
    int NoUrut,
    string PenjualanId = "",
    string NoSep = "",
    bool IsPaymentOverridden = false,
    string PaymentOverrideReason = "",
    string PaymentOverridePharmacistId = "",
    string? LoketKey = null,
    object? ExpectedLoketRowVersion = null);

#endregion

/// <summary>
/// HTTP RESTful API Controller untuk pengelolaan antrean farmasi (Pharmacy Queue System).
/// Menyediakan rute berotorisasi di bawah /api/v1/pharmacy-queue/* untuk intake kiosk,
/// pemuatan worklist, pemanggilan loket fisik, demand mapping, penandaan kehadiran pasien,
/// penyerahan obat di Loket 2, dan query titik layanan.
/// </summary>
[Route("api/v1/pharmacy-queue")]
[ApiController]
[Authorize]
[ServiceFilter(typeof(ApotekExceptionFilter))]
public class PharmacyQueueV1Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserContext _user;
    private readonly ITglJamProvider _tglJamProvider;

    public PharmacyQueueV1Controller(
        IMediator mediator,
        ICurrentUserContext user,
        ITglJamProvider tglJamProvider)
    {
        _mediator = mediator;
        _user = user;
        _tglJamProvider = tglJamProvider;
    }

    /// <summary>
    /// POST /api/v1/pharmacy-queue/intake: Endpoint intake tiket mesin kiosk mandiri.
    /// Mengalokasikan nomor antrean sekuensial on-demand langsung berstatus TAKEN.
    /// Mendukung alur scan nomor registrasi (RegId) untuk klasifikasi otomatis BPJS/Umum.
    /// </summary>
    [HttpPost("intake")]
    [AllowAnonymous]
    public async Task<IActionResult> Intake([FromBody] PharmacyQueueIntakeRequest request)
    {
        if (request == null)
            throw new ApotekDomainException("Request body intake wajib diisi.");

        if (string.IsNullOrWhiteSpace(request.ServicePointId) && string.IsNullOrWhiteSpace(request.RegId))
            throw new ApotekDomainException("Service point ID atau Reg ID wajib diisi.");

        var actor = !string.IsNullOrWhiteSpace(request.UserId) && request.UserId != "KIOSK"
            ? request.UserId
            : (User?.Identity?.IsAuthenticated == true ? AptActor.Require(_user) : "KIOSK");

        var cmd = new PharmacyQueueIntakeCmd(
            ServicePointId: request.ServicePointId ?? string.Empty,
            DepoFarmasiId: request.DepoFarmasiId ?? string.Empty,
            RegId: request.RegId ?? string.Empty,
            UserId: actor);

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// GET /api/v1/pharmacy-queue/worklist: Endpoint pemuatan worklist antrean aktif petugas farmasi.
    /// Mendukung filter tab: Menunggu, Menunggu Telaah, Dilewati, Siap Penjualan, Siap Serah.
    /// Mendukung penyaringan daftar titik layanan spesifik loket (servicePointIds).
    /// </summary>
    [HttpGet("worklist")]
    public async Task<IActionResult> Worklist(
        [FromQuery] string? depoFarmasiId,
        [FromQuery] string? depoId,
        [FromQuery] string? filterTab,
        [FromQuery] ApotekPhaseEnum? phase,
        [FromQuery] string[]? servicePointIds = null)
    {
        var targetDepo = !string.IsNullOrWhiteSpace(depoFarmasiId) ? depoFarmasiId : depoId;
        if (string.IsNullOrWhiteSpace(targetDepo))
            throw new ApotekDomainException("Depo Farmasi ID wajib diisi.");

        var result = await _mediator.Send(new PharmacyQueueWorklistQry(
            targetDepo,
            filterTab,
            phase,
            servicePointIds));
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// POST /api/v1/pharmacy-queue/call-next: Memanggil antrean berikutnya di loket (Loket 1 Penerimaan).
    /// Dapat memanggil tiket TAKEN berikutnya atau tiket spesifik terproteksi token RowVersion.
    /// </summary>
    [HttpPost("call-next")]
    public async Task<IActionResult> CallNext([FromBody] PharmacyQueueCallRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.LoketKey))
            throw new ApotekDomainException("Loket Key wajib diisi.");

        var expectedVersion = ParseRowVersion(request.ExpectedRowVersion);
        var cmd = new PharmacyQueueCallPenerimaanCmd(
            LoketKey: request.LoketKey,
            AntrianId: request.AntrianId,
            NoUrut: request.NoUrut,
            ExpectedRowVersion: expectedVersion,
            UserId: AptActor.Require(_user));

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// POST /api/v1/pharmacy-queue/recall: Memanggil ulang antrean aktif di Loket 1 Penerimaan.
    /// </summary>
    [HttpPost("recall")]
    public async Task<IActionResult> Recall([FromBody] PharmacyQueueRecallRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.LoketKey))
            throw new ApotekDomainException("Loket Key wajib diisi.");

        var rowVersion = ParseRowVersion(request.RowVersion)
            ?? throw new ApotekDomainException("RowVersion token wajib diisi.");

        var cmd = new PharmacyQueueRecallPenerimaanCmd(
            LoketKey: request.LoketKey,
            RowVersion: rowVersion,
            UserId: AptActor.Require(_user));

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// POST /api/v1/pharmacy-queue/skip: Melewati antrean yang tidak merespons di Loket 1 Penerimaan.
    /// </summary>
    [HttpPost("skip")]
    public async Task<IActionResult> Skip([FromBody] PharmacyQueueSkipRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.LoketKey))
            throw new ApotekDomainException("Loket Key wajib diisi.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ApotekDomainException("Alasan lewati antrean (Reason) wajib diisi.");

        var rowVersion = ParseRowVersion(request.RowVersion)
            ?? throw new ApotekDomainException("RowVersion token wajib diisi.");

        var cmd = new PharmacyQueueSkipPenerimaanCmd(
            LoketKey: request.LoketKey,
            Reason: request.Reason,
            RowVersion: rowVersion,
            UserId: AptActor.Require(_user));

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// POST /api/v1/pharmacy-queue/start-service: Menandai dimulainya pelayanan tatap muka di Loket 1 (InService).
    /// </summary>
    [HttpPost("start-service")]
    public async Task<IActionResult> StartService([FromBody] PharmacyQueueStartServiceRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.LoketKey))
            throw new ApotekDomainException("Loket Key wajib diisi.");

        var rowVersion = ParseRowVersion(request.RowVersion)
            ?? throw new ApotekDomainException("RowVersion token wajib diisi.");

        var cmd = new PharmacyQueueStartPenerimaanCmd(
            LoketKey: request.LoketKey,
            RowVersion: rowVersion,
            UserId: AptActor.Require(_user));

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// POST /api/v1/pharmacy-queue/release: Melepaskan klaim loket fisik kembali ke status Idle.
    /// </summary>
    [HttpPost("release")]
    public async Task<IActionResult> Release([FromBody] PharmacyQueueReleaseRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.LoketKey))
            throw new ApotekDomainException("Loket Key wajib diisi.");

        var rowVersion = ParseRowVersion(request.RowVersion)
            ?? throw new ApotekDomainException("RowVersion token wajib diisi.");

        var cmd = new PharmacyQueueReleasePenerimaanCmd(
            LoketKey: request.LoketKey,
            RowVersion: rowVersion,
            UserId: AptActor.Require(_user));

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// POST /api/v1/pharmacy-queue/map-demand: Menautkan tiket antrean fisik ke satu atau beberapa e-resep aktif pasien.
    /// </summary>
    [HttpPost("map-demand")]
    public async Task<IActionResult> MapDemand([FromBody] PharmacyQueueMapDemandRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.AntrianId))
            throw new ApotekDomainException("Antrian ID wajib diisi.");
        if (request.NoUrut <= 0)
            throw new ApotekDomainException("Nomor urut antrean harus lebih besar dari 0.");

        var cmd = new PharmacyQueueMapDemandCmd(
            AntrianId: request.AntrianId,
            NoUrut: request.NoUrut,
            DemandKind: request.DemandKind,
            DemandId: request.DemandId ?? string.Empty,
            DemandIds: request.DemandIds,
            PasienTrackerId: request.PasienTrackerId ?? string.Empty,
            MappingMethod: request.MappingMethod,
            UserId: AptActor.Require(_user));

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// POST /api/v1/pharmacy-queue/flag-pasien-hadir: Menandai kehadiran fisik pasien dan menyiarkan prioritas ke Telaah Resep.
    /// </summary>
    [HttpPost("flag-pasien-hadir")]
    public async Task<IActionResult> FlagPasienHadir([FromBody] PharmacyQueueFlagPasienHadirRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.AntrianId))
            throw new ApotekDomainException("Antrian ID wajib diisi.");
        if (request.NoUrut <= 0)
            throw new ApotekDomainException("Nomor urut antrean harus lebih besar dari 0.");

        var cmd = new PharmacyQueueFlagPasienHadirCmd(
            DemandKind: request.DemandKind,
            DemandId: request.DemandId ?? string.Empty,
            AntrianId: request.AntrianId,
            NoUrut: request.NoUrut,
            UserId: AptActor.Require(_user));

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// POST /api/v1/pharmacy-queue/call-serah: Memanggil nomor antrean di Loket 2 (Serah Obat).
    /// </summary>
    [HttpPost("call-serah")]
    public async Task<IActionResult> CallSerah([FromBody] PharmacyQueueCallRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.LoketKey))
            throw new ApotekDomainException("Loket Key wajib diisi.");

        var expectedVersion = ParseRowVersion(request.ExpectedRowVersion);
        var cmd = new PharmacyQueueCallSerahCmd(
            LoketKey: request.LoketKey,
            AntrianId: request.AntrianId,
            NoUrut: request.NoUrut,
            ExpectedRowVersion: expectedVersion,
            UserId: AptActor.Require(_user));

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// POST /api/v1/pharmacy-queue/complete-handover: Menyelesaikan penyerahan fisik obat di Loket 2 dengan evaluasi kasir/SEP.
    /// </summary>
    [HttpPost("complete-handover")]
    public async Task<IActionResult> CompleteHandover([FromBody] PharmacyQueueCompleteHandoverRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.AntrianId))
            throw new ApotekDomainException("Antrian ID wajib diisi.");
        if (request.NoUrut <= 0)
            throw new ApotekDomainException("Nomor urut antrean harus lebih besar dari 0.");

        var expectedVersion = ParseRowVersion(request.ExpectedLoketRowVersion);
        var cmd = new PharmacyQueueCompleteHandoverCmd(
            AntrianId: request.AntrianId,
            NoUrut: request.NoUrut,
            PenjualanId: request.PenjualanId ?? string.Empty,
            NoSep: request.NoSep ?? string.Empty,
            IsPaymentOverridden: request.IsPaymentOverridden,
            PaymentOverrideReason: request.PaymentOverrideReason ?? string.Empty,
            PaymentOverridePharmacistId: request.PaymentOverridePharmacistId ?? string.Empty,
            UserId: AptActor.Require(_user),
            LoketKey: request.LoketKey,
            ExpectedLoketRowVersion: expectedVersion);

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// GET /api/v1/pharmacy-queue/service-points: Mengembalikan daftar service point titik layanan depo farmasi.
    /// Dapat diakses secara publik oleh mesin kiosk mandiri antrean farmasi.
    /// </summary>
    [HttpGet("service-points")]
    [AllowAnonymous]
    public async Task<IActionResult> ListServicePoints(
        [FromQuery] string? depoFarmasiId,
        [FromQuery] string? depoId,
        [FromQuery] bool activeOnly = true)
    {
        var targetDepo = !string.IsNullOrWhiteSpace(depoFarmasiId) ? depoFarmasiId : depoId;
        var result = await _mediator.Send(new PharmacyQueueServicePointsQry(targetDepo, activeOnly));
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// GET /api/v1/pharmacy-queue/lokets/{loketKey}/service-points: Membaca daftar service point yang dilayani oleh suatu loket fisik.
    /// </summary>
    [HttpGet("lokets/{loketKey}/service-points")]
    public async Task<IActionResult> GetLoketServicePoints([FromRoute] string loketKey)
    {
        if (string.IsNullOrWhiteSpace(loketKey))
            throw new ApotekDomainException("Loket Key wajib diisi.");

        var result = await _mediator.Send(new GetLoketServicePointsQry(loketKey));
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// PUT /api/v1/pharmacy-queue/lokets/{loketKey}/service-points: Memperbarui daftar titik layanan (Service Point) yang dilayani loket fisik.
    /// Dibatasi hanya untuk peran Administrator dan ApotekerSupervisor.
    /// </summary>
    [HttpPut("lokets/{loketKey}/service-points")]
    [Authorize(Roles = "Administrator,ApotekerSupervisor")]
    public async Task<IActionResult> UpdateLoketServicePoints(
        [FromRoute] string loketKey,
        [FromBody] PharmacyQueueUpdateLoketServicePointsRequest request)
    {
        if (string.IsNullOrWhiteSpace(loketKey))
            throw new ApotekDomainException("Loket Key wajib diisi.");
        if (request == null)
            throw new ApotekDomainException("Request body wajib diisi.");

        var rowVersion = ParseRowVersion(request.RowVersion)
            ?? throw new ApotekDomainException("RowVersion token wajib diisi.");

        var cmd = new UpdateLoketServicePointsCmd(
            LoketKey: loketKey,
            ServicePointIds: request.ServicePointIds ?? [],
            RowVersion: rowVersion,
            UserId: AptActor.Require(_user));

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    #region Helper Methods

    private static byte[]? ParseRowVersion(object? raw)
    {
        if (raw is null) return null;
        if (raw is byte[] bytes) return bytes;
        if (raw is string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return null;
            try { return Convert.FromBase64String(str); }
            catch { throw new ApotekDomainException("RowVersion token harus dalam format Base64."); }
        }
        if (raw is JsonElement element)
        {
            if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return null;
            if (element.ValueKind == JsonValueKind.String)
            {
                var s = element.GetString();
                if (string.IsNullOrWhiteSpace(s)) return null;
                try { return Convert.FromBase64String(s); }
                catch { throw new ApotekDomainException("RowVersion token harus dalam format Base64."); }
            }
            if (element.ValueKind == JsonValueKind.Array)
            {
                return element.EnumerateArray().Select(x => x.GetByte()).ToArray();
            }
        }
        return null;
    }

    #endregion
}
