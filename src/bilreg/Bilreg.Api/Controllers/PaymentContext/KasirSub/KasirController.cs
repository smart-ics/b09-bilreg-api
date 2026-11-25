using System.Security.Claims;
using Bilreg.Application.PaymentContext.KasirFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PaymentContext.KasirSub;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class KasirController : Controller
{
    private readonly IMediator _mediator;

    public KasirController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("loket")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMasterLoket()
    {
        var query = new GetMasterLoketKasirQuery();
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("jenis-payment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMasterJenisPayment([FromQuery] string? kategori = null)
    {
        var query = new GetMasterJenisPaymentQuery(kategori);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("order-inbox")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrderInbox(
        [FromQuery] string? keyword = null,
        [FromQuery] string? date = null)
    {
        var query = new GetAntreanOrderKasirQuery(keyword, date);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("billing-unpaid/{regId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBillingUnpaid(string regId)
    {
        if (string.IsNullOrWhiteSpace(regId))
        {
            return BadRequest(new JSend(StatusCodes.Status400BadRequest, "Bad Request", "RegId wajib diisi."));
        }

        var query = new GetBillingUnpaidByRegQuery(regId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpPost]
    [Route("terima")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TerimaKas([FromBody] CreateTerimaKasCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.KasirId))
        {
            var user = GetCurrentUserId();
            if (!string.IsNullOrWhiteSpace(user))
            {
                cmd = cmd with { KasirId = user };
            }
        }
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost]
    [Route("keluar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> KeluarKas([FromBody] CreateKeluarKasCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.KasirId))
        {
            var user = GetCurrentUserId();
            if (!string.IsNullOrWhiteSpace(user))
            {
                cmd = cmd with { KasirId = user };
            }
        }
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost]
    [Route("void")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VoidTransaksi([FromBody] VoidTransaksiKasirCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.PetugasVoid))
        {
            var user = GetCurrentUserId();
            if (!string.IsNullOrWhiteSpace(user))
            {
                cmd = cmd with { PetugasVoid = user };
            }
            else if (!string.IsNullOrWhiteSpace(cmd.SupervisorUserId))
            {
                cmd = cmd with { PetugasVoid = cmd.SupervisorUserId };
            }
        }
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(string id)
    {
        var query = new GetTransaksiKasirDetailQuery(id);
        var result = await _mediator.Send(query);
        if (result is null)
        {
            return NotFound(new JSend(StatusCodes.Status404NotFound, "Not Found", $"Transaksi kasir dengan ID '{id}' tidak ditemukan."));
        }
        return Ok(new JSendOk(result));
    }

    [HttpPost]
    [Route("shift/open")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OpenShift([FromBody] OpenKasirShiftCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.KasirId))
        {
            var user = GetCurrentUserId();
            if (!string.IsNullOrWhiteSpace(user))
            {
                cmd = cmd with { KasirId = user };
            }
        }
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost]
    [Route("shift/close")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CloseShift([FromBody] CloseKasirShiftCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.KasirId))
        {
            var user = GetCurrentUserId();
            if (!string.IsNullOrWhiteSpace(user))
            {
                cmd = cmd with { KasirId = user };
            }
        }
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("shift/active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetShiftActive([FromQuery] string? kasirId = null)
    {
        var currentKasirId = !string.IsNullOrWhiteSpace(kasirId)
            ? kasirId
            : GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(currentKasirId))
        {
            return BadRequest(new JSend(StatusCodes.Status400BadRequest, "Bad Request", "KasirId wajib diisi."));
        }

        var query = new GetKasirShiftActiveQuery(currentKasirId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    private string GetCurrentUserId()
    {
        return User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User?.FindFirst("sub")?.Value
            ?? User?.FindFirst(ClaimTypes.Name)?.Value
            ?? User?.Identity?.Name
            ?? string.Empty;
    }
}
