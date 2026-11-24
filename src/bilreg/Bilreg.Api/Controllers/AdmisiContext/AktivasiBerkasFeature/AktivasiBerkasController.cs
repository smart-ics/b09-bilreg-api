using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.Integration;
using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.AktivasiBerkasFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AktivasiBerkasController : ControllerBase
{
    private readonly IMediator _mediator;

    public AktivasiBerkasController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List([FromQuery] string tglAwal, [FromQuery] string tglAkhir)
    {
        var query = new AktivasiBerkasListQuery(tglAwal, tglAkhir);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new AktivasiBerkasGetQuery(id);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] AktivasiBerkasSaveCommand cmd)
    {
        var executorUserId = !string.IsNullOrWhiteSpace(cmd?.AktifasiUserrId)
            ? cmd.AktifasiUserrId
            : (User?.Identity?.Name ?? string.Empty);

        var finalCmd = cmd is not null && string.IsNullOrWhiteSpace(cmd.AktifasiUserrId) && !string.IsNullOrWhiteSpace(executorUserId)
            ? cmd with { AktifasiUserrId = executorUserId }
            : cmd;

        var result = await _mediator.Send(finalCmd!);
        return ToMutationActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] AktivasiBerkasUpdateBody body)
    {
        var executorUserId = !string.IsNullOrWhiteSpace(body?.AktifasiUserrId)
            ? body.AktifasiUserrId
            : (User?.Identity?.Name ?? string.Empty);

        var cmd = new AktivasiBerkasUpdateCommand(
            id,
            body?.TglJam ?? string.Empty,
            body?.ReqUserrId ?? string.Empty,
            body?.TglAktif ?? string.Empty,
            executorUserId,
            body?.LayananId ?? string.Empty,
            body?.ListRegister ?? []
        );

        var result = await _mediator.Send(cmd);
        return ToMutationActionResult(result);
    }

    private IActionResult ToMutationActionResult(AktivasiBerkasMutationResult result)
    {
        if (!result.Success)
        {
            var statusCode = result.StatusCode is >= 400 and < 600
                ? result.StatusCode.Value
                : StatusCodes.Status400BadRequest;
            var jsendStatus = statusCode >= 500 ? "error" : "fail";
            return StatusCode(statusCode, new JSend(statusCode, jsendStatus, result.Message));
        }

        return Ok(new JSendOk(result));
    }

    [HttpGet("find-register")]
    public async Task<IActionResult> FindRegister([FromQuery] string keyword)
    {
        var query = new AktivasiBerkasFindRegisterQuery(keyword);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet("list-reaktif")]
    public async Task<IActionResult> ListReaktif([FromQuery] string? layananId, CancellationToken ct = default)
    {
        var query = new AktivasiBerkasListReaktifQuery(layananId);
        var result = await _mediator.Send(query, ct);
        return Ok(new JSendOk(result));
    }

    [HttpPost("deactivate")]
    public async Task<IActionResult> Deactivate([FromBody] AktivasiBerkasDeactivateBody body, CancellationToken ct = default)
    {
        var regId = body?.RegId ?? string.Empty;
        var executorUserId = !string.IsNullOrWhiteSpace(body?.UserrId)
            ? body.UserrId
            : (User?.Identity?.Name ?? string.Empty);

        var cmd = new AktivasiBerkasDeactivateCommand(regId, executorUserId);
        var result = await _mediator.Send(cmd, ct);
        return ToMutationActionResult(result);
    }

    [HttpDelete("cleanup")]
    public async Task<IActionResult> CleanUp(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new AktivasiBerkasCleanUpCommand(), cancellationToken);
        return ToMutationActionResult(result);
    }
}

public record AktivasiBerkasUpdateBody(
    string TglJam,
    string ReqUserrId,
    string TglAktif,
    string AktifasiUserrId,
    string LayananId,
    IEnumerable<AktivasiBerkasRegisterRefDto>? ListRegister = null,
    string? Id = null
);

public record AktivasiBerkasDeactivateBody(
    string RegId,
    string? UserrId = null
)
{
    public AktivasiBerkasDeactivateBody() : this(string.Empty, null) { }
}
