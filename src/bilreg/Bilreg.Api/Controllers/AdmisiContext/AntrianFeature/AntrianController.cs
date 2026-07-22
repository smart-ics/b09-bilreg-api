using Bilreg.Application.AdmisiContext.AntrianFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.AntrianFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AntrianController : Controller
{
    private readonly IMediator _mediator;

    public AntrianController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("quota/{dokterId}/{tglPraktek}/{jamMulai}")]
    public async Task<IActionResult> GetLastNumber(string dokterId, string tglPraktek, string jamMulai)
    {
        var query = new AntrianGetQuotaQuery(dokterId, tglPraktek, jamMulai);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("genNewNumber")]
    public async Task<IActionResult> GetAvailabelNumberQueue(AntrianGenNewNumberCommand cmd)
    {
        var responst = await _mediator.Send(cmd);
        return Ok(new JSendOk(responst));
    }

    [HttpGet]
    [Route("pasien/{tglYmd}")]
    public async Task<IActionResult> ListPasien(string tglYmd)
    {
        var query = new QuePasienListQuery(tglYmd);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response)); 
    }

    [HttpGet]
    [Route("header/{tglYmd}/list")]
    public async Task<IActionResult> ListHeader(string tglYmd)
    {
        var query = new  QueListAntrianHeaderQuery(tglYmd);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetAntrian(string id)
    {
        var query = new QueGetAntrianQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("anonymous-intake")]
    public async Task<IActionResult> AnonymousIntake(QueAnonymousIntakeCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("start")]
    public async Task<IActionResult> Start(AdmissionQueueStartCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPatch]
    [Route("mulaiPeriksa/{antrianId}/{noUrut:int}")]
    public async Task<IActionResult> MulaiPeriksa(string antrianId, int noUrut)
    {
        var response = await _mediator.Send(new QueMulaiPeriksaCmd(antrianId, noUrut));
        return Ok(new JSendOk(response));
    }

    [HttpPatch]
    [Route("selesaiPeriksa/{antrianId}/{noUrut:int}")]
    public async Task<IActionResult> SelesaiPeriksa(string antrianId, int noUrut)
    {
        var response = await _mediator.Send(new QueSelesaiPeriksaCmd(antrianId, noUrut));
        return Ok(new JSendOk(response));
    }

    [HttpPatch]
    [Route("fixOutstandingReference")]
    public async Task<IActionResult> FixOutstandingReference(QueFixOutstandingReferenceCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    // [HttpPost]
    // [Route("mapMigrasi")]
    // public async Task<IActionResult> MigrasiMapHdr(AntrianMapHdrMigrasiCmd cmd)
    // {
    //     await _mediator.Send(cmd);
    //     return Ok(new JSendOk("Done"));
    // }
}
