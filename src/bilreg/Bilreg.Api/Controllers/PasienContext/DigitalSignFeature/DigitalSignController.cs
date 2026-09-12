using Bilreg.Application.PasienContext.DigitalSignFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;
using System.Net;
using System.Text.Json.Serialization;

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
                    FailedResult(result, ((int)HttpStatusCode.NotFound).ToString()));

            case HiDokPatientSignerResolveStatus.NotVerified:
                return StatusCode(428, FailedResult(result, "428"));

            case HiDokPatientSignerResolveStatus.ProvisionFailed:
                return StatusCode((int)HttpStatusCode.BadGateway,
                    FailedResult(result, ((int)HttpStatusCode.BadGateway).ToString()));

            default:
                return StatusCode((int)HttpStatusCode.BadGateway,
                    new JSendFailed(new ArgumentException(result.ErrorMessage)));
        }
    }

    private static JSendModel FailedResult(ResolvePatientSignerResponse result, string code)
    {
        object data;
        if (result.Patient is null)
        {
            data = new { message = result.ErrorMessage };
        }
        else
        {
            data = new
            {
                message = result.ErrorMessage,
                patient = new ResolvePatientSignerPatientDto(
                    result.Patient.UserrId,
                    result.Patient.NoMR,
                    result.Patient.PasienName,
                    result.Patient.Alamat,
                    result.Patient.TglLahir,
                    result.Patient.NoTelp,
                    result.Patient.NoKTP,
                    result.Patient.RSID)
            };
        }

        return new JSendModel { status = "failed", code = code, data = data };
    }
}

public record ResolvePatientSignerResultDto(string UserrId, string SignerId);

public record ResolvePatientSignerPatientDto(
    [property: JsonPropertyName("UserrID")] string UserrId,
    [property: JsonPropertyName("NoMR")] string NoMR,
    [property: JsonPropertyName("PasienName")] string PasienName,
    [property: JsonPropertyName("Alamat")] string Alamat,
    [property: JsonPropertyName("TglLahir")] string TglLahir,
    [property: JsonPropertyName("NoTelp")] string NoTelp,
    [property: JsonPropertyName("NoKTP")] string NoKTP,
    [property: JsonPropertyName("RSID")] string RSID);