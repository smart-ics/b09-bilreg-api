 //TODO: Refactor AntrianMap Model

using Bilreg.Api.Helpers;
using Bilreg.Api.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RegFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.RegFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RegController : Controller
{
    private readonly IMediator _mediator;
    private readonly IAdmissionQueueWorkstationResolver _workstationResolver;

    public RegController(
        IMediator mediator,
        IAdmissionQueueWorkstationResolver workstationResolver)
    {
        _mediator = mediator;
        _workstationResolver = workstationResolver;
    }

    [HttpPost]
    [Route("rajalWalkIn")]
    public async Task<IActionResult> Save(RegJalanWalkInCommand cmd)
    {
        cmd = WithAdmissionQueueBehavior(cmd, isDirect: false);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost]
    [Route("rajalWalkIn/direct")]
    public async Task<IActionResult> SaveDirect(RegJalanWalkInCommand cmd)
    {
        cmd = WithAdmissionQueueBehavior(cmd, isDirect: true);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost]
    [Route("rajalByBooking")]
    public async Task<IActionResult> Save(RegJalanByBookingCmd cmd)
    {
        cmd = WithAdmissionQueueBehavior(cmd, isDirect: false);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost]
    [Route("rajalByBooking/direct")]
    public async Task<IActionResult> SaveDirect(RegJalanByBookingCmd cmd)
    {
        cmd = WithAdmissionQueueBehavior(cmd, isDirect: true);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPatch]
    [Route("setDataEligibility")]
    public async Task<IActionResult> SetDataEligibility(RegSetDataEligibilityCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch]
    [Route("ubahKunjungan")]
    public async Task<IActionResult> UbahKunjungan(RegJalanUbahKunjunganCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPatch]
    [Route("ubahJaminan")]
    public async Task<IActionResult> UbahJaminan(RegJalanUbahJaminanCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch]
    [Route("rajalBatal")]
    public async Task<IActionResult> Void(RegBatalRequestDto req)
    {
        var userAgent = HttpHelper.GetUserAgent(Request);
        var remoteIpAddress = HttpHelper.GetIpAddress(Request, HttpContext);

        var cmd = new RegJalanBatalCmd(req.RegId, req.UserId, req.VoidReason, remoteIpAddress, userAgent);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost]
    [Route("darurat")]
    public async Task<IActionResult> RegRadar(RegDaruratCreateCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost]
    [Route("regAktif/add")]
    public async Task<IActionResult> RegAktifAdd(RegRegAktifAddCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
    [HttpPatch]
    [Route("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(string id)
    {
        var userAgent = HttpHelper.GetUserAgent(Request);
        var remoteIpAddress = HttpHelper.GetIpAddress(Request, HttpContext);

        var cmd = new RegRegAktifRemoveCmd(id, remoteIpAddress, userAgent);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new RegGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("{layananId}/layanan")]
    public async Task<IActionResult> ListAktif(string layananId)
    {
        var query = new RegAktifLayananListQuery(layananId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("aktif/{pasienId}")]
    public async Task<IActionResult> ListAktifByMr(string pasienId)
    {
        var query = new RegAktifByMrListQuery(pasienId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("aktif/{jenisReg}/jenisReg")]
    public async Task<IActionResult> ListAktifByJenisReg(int jenisReg)
    {
        var query = new RegAktifByJenisRegListQuery(jenisReg);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("listAktif")]
    public async Task<IActionResult> ListAktif()
    {
        var query = new RegRegAktifListQuery();
        var resutl = await _mediator.Send(query);
        return Ok(new JSendOk(resutl));
    }

    private RegJalanWalkInCommand WithAdmissionQueueBehavior(RegJalanWalkInCommand cmd, bool isDirect)
    {
        var behavior = AdmissionRegistrationQueueContextResolver.ResolveBehavior(
            cmd.AdmissionAntrianId, cmd.AdmissionNoUrut, cmd.AdmissionExpectedRowVersion, isDirect);

        return cmd with
        {
            AdmissionQueueBehavior = behavior,
            AdmissionLoketKey = behavior == RegistrationAdmissionQueueBehavior.QueueLinked
                ? _workstationResolver.Resolve(Request, null).LoketKey
                : null
        };
    }

    private RegJalanByBookingCmd WithAdmissionQueueBehavior(RegJalanByBookingCmd cmd, bool isDirect)
    {
        var behavior = AdmissionRegistrationQueueContextResolver.ResolveBehavior(
            cmd.AdmissionAntrianId, cmd.AdmissionNoUrut, cmd.AdmissionExpectedRowVersion, isDirect);

        return cmd with
        {
            AdmissionQueueBehavior = behavior,
            AdmissionLoketKey = behavior == RegistrationAdmissionQueueBehavior.QueueLinked
                ? _workstationResolver.Resolve(Request, null).LoketKey
                : null
        };
    }
}

public record RegBatalRequestDto(string RegId, string UserId, string VoidReason);
