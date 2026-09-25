using Bilreg.Api.Filters;
using Bilreg.Application.PasienContext.DigitalSignFeature;
using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature.UseCases;
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

    [HttpPost("~/api/admisi-ranap/digital-sign")]
    [ServiceFilter(typeof(AdmisiRanapEnabledFilter))]
    public async Task<IActionResult> RecordRanapDigitalSign([FromBody] AdmRecordDigitalSignBody body)
    {
        try
        {
            var cmd = new AdmRecordDigitalSignCmd(
                body.RegId,
                body.HisReference ?? string.Empty,
                body.DokumenId,
                body.SigningRequestId,
                body.SignerId ?? string.Empty,
                body.FileName ?? string.Empty,
                body.UserId);
            var result = await _mediator.Send(cmd);
            return Ok(new JSendOk(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new JSendFailed(new Exception(ex.Message)));
        }
        catch (ArgumentException ex)
        {
            return StatusCode(StatusCodes.Status422UnprocessableEntity, new JSendFailed(ex));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new JSendFailed(ex));
        }
    }

    [HttpGet("~/api/admisi-ranap/digital-sign")]
    [ServiceFilter(typeof(AdmisiRanapEnabledFilter))]
    public async Task<IActionResult> GetRanapDigitalSign(
        [FromQuery] string regId,
        [FromQuery] string dokumenId)
    {
        if (string.IsNullOrWhiteSpace(regId))
            return BadRequest(new JSendFailed(new ArgumentException("regId wajib diisi.")));
        if (string.IsNullOrWhiteSpace(dokumenId))
            return BadRequest(new JSendFailed(new ArgumentException("dokumenId wajib diisi.")));

        try
        {
            var result = await _mediator.Send(new AdmGetDigitalSignQry(regId, dokumenId));
            var item = result.Items.SingleOrDefault();
            return item is null
                ? NotFound(new JSend(StatusCodes.Status404NotFound, "Not Found",
                    $"DigitalSign regId '{regId}' dokumenId '{dokumenId}' was not found."))
                : Ok(new JSendOk(item));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new JSendFailed(ex));
        }
    }

    [HttpGet("~/api/admisi-ranap/digital-sign/list")]
    [ServiceFilter(typeof(AdmisiRanapEnabledFilter))]
    public async Task<IActionResult> ListRanapDigitalSign([FromQuery] string regId)
    {
        if (string.IsNullOrWhiteSpace(regId))
            return BadRequest(new JSendFailed(new ArgumentException("regId wajib diisi.")));

        try
        {
            var result = await _mediator.Send(new AdmGetDigitalSignQry(regId));
            return Ok(new JSendOk(result.Items));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new JSendFailed(ex));
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

public record AdmRecordDigitalSignBody(
    string RegId,
    string? HisReference,
    string DokumenId,
    string SigningRequestId,
    string? SignerId,
    string? FileName,
    string UserId);

public record ResolvePatientSignerPatientDto(
    [property: JsonPropertyName("UserrID")] string UserrId,
    [property: JsonPropertyName("NoMR")] string NoMR,
    [property: JsonPropertyName("PasienName")] string PasienName,
    [property: JsonPropertyName("Alamat")] string Alamat,
    [property: JsonPropertyName("TglLahir")] string TglLahir,
    [property: JsonPropertyName("NoTelp")] string NoTelp,
    [property: JsonPropertyName("NoKTP")] string NoKTP,
    [property: JsonPropertyName("RSID")] string RSID);