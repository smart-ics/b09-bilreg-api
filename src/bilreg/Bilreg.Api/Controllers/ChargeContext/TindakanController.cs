using Bilreg.Api.Helpers;
using Bilreg.Application.BedUsageContext.BhpRanapFeature.UseCases;
using Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.ChargeContext;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TindakanController : Controller
{
    private readonly IMediator _mediator;

    public TindakanController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> Create(TdkCreateTindakanCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    public Task<IActionResult> CreateTindakan(TdkCreateTindakanCmd cmd) => Create(cmd);

    [HttpPost]
    [Route("save")]
    public async Task<IActionResult> SaveTindakan(TdkSaveTindakanCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("update-petugas")]
    [HttpPatch("update-petugas")]
    [HttpPost("update-dokter")]
    [HttpPatch("update-dokter")]
    public async Task<IActionResult> UpdatePetugas([FromBody] TindakanUpdatePetugasRequest req)
    {
        var resolvedTindakanId = !string.IsNullOrWhiteSpace(req.TindakanId)
            ? req.TindakanId
            : (req.TransaksiId ?? "");

        var uid = !string.IsNullOrWhiteSpace(req.UserId)
            ? req.UserId
            : (User.Identity?.Name ?? "admin");

        var cmd = new TdkUpdatePetugasCmd(resolvedTindakanId, req.PpaId, uid);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    public Task<IActionResult> UpdatePetugasMedis([FromBody] TindakanUpdatePetugasRequest req) => UpdatePetugas(req);

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var query = new TindakanGetQuery(id);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    public Task<IActionResult> GetTindakan(string id) => Get(id);

    [HttpGet]
    [Route("list/{regId}")]
    public async Task<IActionResult> List(string regId, [FromQuery] string? pakaiBedId = null)
    {
        var query = new TindakanListQuery(regId, pakaiBedId ?? "");
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("tdkJual/list/{regId}")]
    public async Task<IActionResult> ListTdkJual(string regId)
    {
        var query = new TdkListTindakanJualQuery(regId);
        var response = await _mediator.Send(query); 
        return Ok(new JSendOk(response));
    }

    public Task<IActionResult> ListTindakanJual(string regId) => ListTdkJual(regId);
    
    [HttpPatch]
    [Route("batal")]
    [HttpPatch("void")]
    [HttpPost("batal")]
    [HttpPost("delete")]
    [HttpPost("void")]
    [HttpDelete("delete")]
    [HttpDelete("batal")]
    public async Task<IActionResult> Batal([FromBody] TindakanVoidRequest req)
    {
        return await ExecuteVoid(req);
    }

    [HttpDelete("{tindakanId}")]
    [HttpDelete("delete/{tindakanId}")]
    public async Task<IActionResult> DeleteById(
        string tindakanId,
        [FromQuery] string? userId = null,
        [FromQuery] string? voidReason = null,
        [FromBody] TindakanVoidRequest? body = null)
    {
        var resolvedTindakanId = !string.IsNullOrWhiteSpace(tindakanId)
            ? tindakanId
            : (!string.IsNullOrWhiteSpace(body?.TindakanId) ? body.TindakanId : (body?.TransaksiId ?? ""));

        var uid = !string.IsNullOrWhiteSpace(body?.UserId)
            ? body.UserId
            : (!string.IsNullOrWhiteSpace(userId) ? userId : (User.Identity?.Name ?? "admin"));

        var reason = !string.IsNullOrWhiteSpace(body?.VoidReason)
            ? body.VoidReason
            : (!string.IsNullOrWhiteSpace(body?.Alasan)
                ? body.Alasan
                : (!string.IsNullOrWhiteSpace(voidReason) ? voidReason : "Pembatalan tindakan"));

        var req = new TindakanVoidRequest(resolvedTindakanId, uid, reason);
        return await ExecuteVoid(req);
    }

    private async Task<IActionResult> ExecuteVoid(TindakanVoidRequest req)
    {
        try
        {
            var userAgent = HttpContext is null ? "" : HttpHelper.GetUserAgent(Request);
            var remoteIpAddress = HttpContext is null ? "0.0.0.0" : HttpHelper.GetIpAddress(Request, HttpContext);

            var resolvedTindakanId = !string.IsNullOrWhiteSpace(req.TindakanId)
                ? req.TindakanId
                : (req.TransaksiId ?? "");

            var uid = !string.IsNullOrWhiteSpace(req.UserId)
                ? req.UserId
                : (User.Identity?.Name ?? "admin");

            var reason = !string.IsNullOrWhiteSpace(req.VoidReason)
                ? req.VoidReason
                : (!string.IsNullOrWhiteSpace(req.Alasan) ? req.Alasan : "Pembatalan tindakan");

            if (resolvedTindakanId.StartsWith("BHR", StringComparison.OrdinalIgnoreCase))
            {
                var bhCmd = new BhpRanapVoidCmd(resolvedTindakanId, uid, reason);
                await _mediator.Send(bhCmd);
                return Ok(new JSendOk("Done"));
            }

            var cmd = new TindakanVoidCmd(resolvedTindakanId, uid, reason, remoteIpAddress, userAgent);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(StatusCodes.Status404NotFound, new JSend(StatusCodes.Status404NotFound, "FAIL", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return StatusCode(StatusCodes.Status400BadRequest, new JSend(StatusCodes.Status400BadRequest, "FAIL", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status422UnprocessableEntity, new JSend(StatusCodes.Status422UnprocessableEntity, "FAIL", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new JSend(StatusCodes.Status500InternalServerError, "ERROR", ex.Message));
        }
    }
}

public record TindakanVoidRequest(
    string? TindakanId, 
    string? UserId = null, 
    string? VoidReason = null,
    string? TransaksiId = null,
    string? Alasan = null);

public record TindakanUpdatePetugasRequest(
    string TindakanId,
    string PpaId,
    string? UserId = null,
    string? TransaksiId = null);
