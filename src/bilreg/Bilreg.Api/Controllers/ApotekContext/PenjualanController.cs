using Bilreg.Application.ApotekContext.PenjualanFeature.UseCases;
using Bilreg.Application.Shared;
using Bilreg.Domain.ApotekContext.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Api.Controllers.ApotekContext;

#region Request DTOs

/// <summary>
/// HTTP Request body untuk endpoint penerbitan tagihan penjualan Jenis 1 (berbasis E-Resep).
/// </summary>
public record PenjualanCreateFromTelaahRequest(
    string ResepId,
    string? UserId = null,
    string? LayananId = null,
    string? TipeJaminanId = null,
    string? KelasId = null,
    DateTime? TransactionDate = null,
    bool? IsRanap = null,
    IReadOnlyList<string>? NonFornasBrgIds = null);

/// <summary>
/// HTTP Request body untuk endpoint penerbitan tagihan penjualan Jenis 2 (langsung registrasi).
/// </summary>
public record PenjualanCreateDirectRequest(
    string RegId,
    string DokterId,
    string LayananId,
    IReadOnlyList<PenjualanDirectItemInput> Items,
    string? UserId = null,
    string? TipeJaminanId = null,
    string? KelasId = null,
    DateTime? TransactionDate = null,
    string LayananResepId = "",
    decimal DiskonLain = 0,
    decimal BiayaLain = 0,
    decimal Pembulatan = 0,
    bool? IsRanap = null,
    string? PenjualanId = null);

/// <summary>
/// HTTP Request body untuk endpoint penerbitan tagihan penjualan Jenis 3 (bebas walk-in OTC).
/// </summary>
public record PenjualanCreateWalkInRequest(
    string PembeliName,
    string LayananId,
    IReadOnlyList<PenjualanWalkInItemInput> Items,
    string? UserId = null,
    string PembeliAlamat = "",
    string PembeliKota = "",
    string Sex = "",
    DateTime? TransactionDate = null,
    decimal DiskonLain = 0,
    decimal BiayaLain = 0,
    decimal Pembulatan = 0,
    string? PenjualanId = null);

/// <summary>
/// HTTP Request body untuk endpoint pembatalan tagihan penjualan (Soft-Void).
/// </summary>
public record PenjualanVoidRequest(
    string Reason = "",
    string? UserId = null,
    DateTime? VoidAt = null);

#endregion

/// <summary>
/// HTTP RESTful API Controller untuk transaksi Penjualan Farmasi (Sales Bill) Apotek.
/// Menyediakan rute berotorisasi di bawah /api/v1/apotek/penjualan/* untuk 3 jenis tagihan,
/// pembatalan soft-void, query detail, riwayat registrasi, dan antrean worklist operasional.
/// </summary>
[Route("api/v1/apotek/penjualan")]
[ApiController]
[Authorize]
[ServiceFilter(typeof(ApotekExceptionFilter))]
public class PenjualanController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserContext _user;
    private readonly ITglJamProvider _tglJamProvider;

    public PenjualanController(
        IMediator mediator,
        ICurrentUserContext user,
        ITglJamProvider tglJamProvider)
    {
        _mediator = mediator;
        _user = user;
        _tglJamProvider = tglJamProvider;
    }

    /// <summary>
    /// POST /api/v1/apotek/penjualan/from-telaah: Menerima resepId, menerbitkan tagihan Jenis 1.
    /// Mendukung pemisahan otomatis tagihan multi-penjamin BPJS (Fornas vs Non-Fornas) menjadi 2 dokumen DU terpisah.
    /// </summary>
    [HttpPost("from-telaah")]
    public async Task<IActionResult> CreateFromTelaah([FromBody] PenjualanCreateFromTelaahRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ResepId))
            throw new ApotekDomainException("Nomor resep wajib diisi.");

        var cmd = new PenjualanCreateFromTelaahCmd(
            UserId: AptActor.Require(_user),
            ResepId: request.ResepId,
            LayananId: request.LayananId,
            TipeJaminanId: request.TipeJaminanId,
            KelasId: request.KelasId,
            TransactionDate: request.TransactionDate ?? _tglJamProvider.Now,
            IsRanap: request.IsRanap,
            NonFornasBrgIds: request.NonFornasBrgIds);

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// POST /api/v1/apotek/penjualan/direct: Menerima regId, dokter, dan daftar obat/BMHP untuk tagihan Jenis 2.
    /// </summary>
    [HttpPost("direct")]
    public async Task<IActionResult> CreateDirect([FromBody] PenjualanCreateDirectRequest request)
    {
        if (request == null)
            throw new ApotekDomainException("Data penjualan langsung tidak boleh kosong.");
        if (string.IsNullOrWhiteSpace(request.RegId))
            throw new ApotekDomainException("Nomor registrasi wajib diisi.");
        if (string.IsNullOrWhiteSpace(request.DokterId))
            throw new ApotekDomainException("Dokter pemeriksa wajib diisi.");
        if (string.IsNullOrWhiteSpace(request.LayananId))
            throw new ApotekDomainException("Layanan apotek wajib diisi.");
        if (request.Items == null || request.Items.Count == 0)
            throw new ApotekDomainException("Penjualan langsung registrasi wajib memiliki minimal satu baris item.");

        var cmd = new PenjualanCreateDirectCmd(
            UserId: AptActor.Require(_user),
            RegId: request.RegId,
            DokterId: request.DokterId,
            LayananId: request.LayananId,
            Items: request.Items,
            TipeJaminanId: request.TipeJaminanId,
            KelasId: request.KelasId,
            TransactionDate: request.TransactionDate ?? _tglJamProvider.Now,
            LayananResepId: request.LayananResepId,
            DiskonLain: request.DiskonLain,
            BiayaLain: request.BiayaLain,
            Pembulatan: request.Pembulatan,
            IsRanap: request.IsRanap,
            PenjualanId: request.PenjualanId);

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// POST /api/v1/apotek/penjualan/walk-in: Menerima nama pembeli luar dan daftar item untuk tagihan Jenis 3.
    /// </summary>
    [HttpPost("walk-in")]
    public async Task<IActionResult> CreateWalkIn([FromBody] PenjualanCreateWalkInRequest request)
    {
        if (request == null)
            throw new ApotekDomainException("Data penjualan walk-in tidak boleh kosong.");
        if (string.IsNullOrWhiteSpace(request.PembeliName))
            throw new ApotekDomainException("Nama pembeli walk-in wajib diisi.");
        if (string.IsNullOrWhiteSpace(request.LayananId))
            throw new ApotekDomainException("Layanan apotek wajib diisi.");
        if (request.Items == null || request.Items.Count == 0)
            throw new ApotekDomainException("Penjualan walk-in bebas wajib memiliki minimal satu baris item.");

        var cmd = new PenjualanCreateWalkInCmd(
            UserId: AptActor.Require(_user),
            PembeliName: request.PembeliName,
            LayananId: request.LayananId,
            Items: request.Items,
            PembeliAlamat: request.PembeliAlamat,
            PembeliKota: request.PembeliKota,
            Sex: request.Sex,
            TransactionDate: request.TransactionDate ?? _tglJamProvider.Now,
            DiskonLain: request.DiskonLain,
            BiayaLain: request.BiayaLain,
            Pembulatan: request.Pembulatan,
            PenjualanId: request.PenjualanId);

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// POST /api/v1/apotek/penjualan/{penjualanId}/void: Menerima alasan pembatalan untuk soft-void.
    /// </summary>
    [HttpPost("{penjualanId}/void")]
    public async Task<IActionResult> Void(string penjualanId, [FromBody] PenjualanVoidRequest? request)
    {
        if (string.IsNullOrWhiteSpace(penjualanId))
            throw new ApotekDomainException("ID Penjualan wajib diisi.");

        var reason = request?.Reason;
        if (string.IsNullOrWhiteSpace(reason))
            throw new ApotekDomainException("Alasan pembatalan penjualan wajib diisi.");

        var cmd = new PenjualanVoidCmd(
            UserId: AptActor.Require(_user),
            PenjualanId: penjualanId,
            Reason: reason,
            VoidAt: request?.VoidAt ?? _tglJamProvider.Now);

        await _mediator.Send(cmd);
        return Ok(new JSendOk(new { message = "Penjualan voided successfully", penjualanId }));
    }

    /// <summary>
    /// GET /api/v1/apotek/penjualan/{penjualanId}: Mengembalikan data utuh dokumen tagihan.
    /// </summary>
    [HttpGet("{penjualanId}")]
    public async Task<IActionResult> GetById(string penjualanId)
    {
        if (string.IsNullOrWhiteSpace(penjualanId))
            throw new ApotekDomainException("ID Penjualan wajib diisi.");

        var result = await _mediator.Send(new PenjualanGetByIdQry(penjualanId));
        if (result == null)
            throw new KeyNotFoundException($"Transaksi penjualan '{penjualanId}' tidak ditemukan.");

        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// GET /api/v1/apotek/penjualan/by-reg/{regId}: Mengembalikan daftar tagihan aktif pasien.
    /// </summary>
    [HttpGet("by-reg/{regId}")]
    public async Task<IActionResult> ListByReg(string regId)
    {
        if (string.IsNullOrWhiteSpace(regId))
            throw new ApotekDomainException("Nomor registrasi wajib diisi.");

        var result = await _mediator.Send(new PenjualanListByRegQry(regId));
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// GET /api/v1/apotek/penjualan/worklist: Mengembalikan worklist tagihan penjualan apotek.
    /// </summary>
    [HttpGet("worklist")]
    public async Task<IActionResult> Worklist(
        [FromQuery] DateOnly? businessDate,
        [FromQuery] int? statusFilter)
    {
        var date = businessDate ?? DateOnly.FromDateTime(_tglJamProvider.Now);
        var result = await _mediator.Send(new PenjualanWorklistQry(date, statusFilter));
        return Ok(new JSendOk(result));
    }

    /// <summary>
    /// GET /api/v1/apotek/penjualan/search-resep: Pencarian universal e-resep aktif hari ini yang belum tertagih.
    /// Multi-parameter: No RM, No Registrasi, Nama Pasien, No Resep.
    /// </summary>
    [HttpGet("search-resep")]
    public async Task<IActionResult> SearchResep(
        [FromQuery] string? q,
        [FromQuery] string? query,
        [FromQuery] DateOnly? date)
    {
        var searchQuery = !string.IsNullOrWhiteSpace(q) ? q : query ?? string.Empty;
        var targetDate = date ?? DateOnly.FromDateTime(_tglJamProvider.Now);
        var result = await _mediator.Send(new PenjualanSearchResepQry(searchQuery, targetDate));
        return Ok(new JSendOk(result));
    }
}

