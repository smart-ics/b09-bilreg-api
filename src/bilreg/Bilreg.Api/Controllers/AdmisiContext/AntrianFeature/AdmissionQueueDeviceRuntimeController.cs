using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.AntrianFeature;

[ApiController]
[Authorize]
[Route("api/v1/admission-queue")]
public sealed class AdmissionQueueDeviceRuntimeController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdmissionQueueDeviceRuntimeController(IMediator mediator) => _mediator = mediator;

    [HttpGet("workstations/available")]
    public async Task<IActionResult> AvailableWorkstations(CancellationToken ct) =>
        Ok(new JSendOk(await _mediator.Send(new ListAvailableWorkstationsQry(), ct)));

    [HttpGet("workstations/{workstationKey}/context")]
    public async Task<IActionResult> WorkstationContext(string workstationKey, CancellationToken ct) =>
        Ok(new JSendOk(await _mediator.Send(new GetWorkstationContextQry(workstationKey), ct)));

    [HttpGet("devices/displays/{displayId}")]
    public async Task<IActionResult> DisplayBootConfig(string displayId, CancellationToken ct) =>
        Ok(new JSendOk(await _mediator.Send(new GetDisplayBootConfigQry(displayId), ct)));
}
