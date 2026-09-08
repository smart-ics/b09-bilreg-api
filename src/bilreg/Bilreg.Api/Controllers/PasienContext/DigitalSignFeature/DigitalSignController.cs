using Bilreg.Application.PasienContext.DigitalSignFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;
using System.Net;

namespace Bilreg.Api.Controllers.PasienContext.DigitalSignFeature;

[Route("api/digital-sign")]
[ApiController]
[Authorize]
public class DigitalSignController : Controller
{
    private readonly IMediator _mediator;

    public DigitalSignController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("patient-signers/resolve")]
    public async Task<IActionResult> ResolvePatientSigner([FromQuery] string mr)
    {
        var query = new ResolvePatientSignerQuery(mr);
        var result = await _mediator.Send(query);

        switch (result.Status)
        {
            case HiDokPatientSignerResolveStatus.Success:
                return Ok(new JSendOk(new ResolvePatientSignerResultDto(
                    result.UserrId, result.SignerId)));

            case HiDokPatientSignerResolveStatus.NotFound:
                return StatusCode((int)HttpStatusCode.NotFound,
                    new JSendFailed(new ArgumentException(result.ErrorMessage)));

            case HiDokPatientSignerResolveStatus.ProvisionFailed:
                return StatusCode((int)HttpStatusCode.BadGateway,
                    new JSendFailed(new ArgumentException(result.ErrorMessage)));

            default:
                return StatusCode((int)HttpStatusCode.BadGateway,
                    new JSendFailed(new ArgumentException(result.ErrorMessage)));
        }
    }
}

public record ResolvePatientSignerResultDto(string UserrId, string SignerId);