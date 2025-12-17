using Bilreg.Application.AdmisiContext.BookingFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.BookingFeature;

[Route("api/[controller]")]
[ApiController]
public class BookingController : ControllerBase
{
    private readonly IMediator _mediator;

    public BookingController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    public async Task<IActionResult> Save(BookingCreateCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPatch]
    [Route("genPasien")]
    public async Task<IActionResult> ResolvePasienId(BokGenPasienFromBookingCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }
    
    [HttpPatch]
    [Route("resolvePasienId")]
    public async Task<IActionResult> ResolvePasienId(BookingResolvePasienIdCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost]
    [Route("createFromHidok")]
    public async Task<IActionResult> CreateFromHidok(BookingCreateFromHidokCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPatch]
    [Route("setQrExt")]
    public async Task<IActionResult> SetQrExt(BookingSetExternalAppCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> Delete(string id)
    { 
        var cmd = new BookingDeleteCmd(id);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpDelete]
    [Route("{bookingIdHidok}/hidok")]
    public async Task<IActionResult> DeleteFromHidok(string bookingIdHidok)
    {
        var cmd = new BookingDeleteFromHidokCmd(bookingIdHidok);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new BookingGetQuery(id);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("list/{tglYmd}/{dokterId}")]
    public async Task<IActionResult> ListBooking(string tglYmd, string dokterId)
    {
        var query = new BookingDokterListQuery(tglYmd, dokterId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }
    [HttpGet]
    [Route("list/{tglYmd}")]
    public async Task<IActionResult> ListAllBooking(string tglYmd)
    {
        var query = new BookingPeriodeListQuery(tglYmd);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("search/{tglBerobat}/{keyword}")]
    public async Task<IActionResult> SearchByQr(string tglBerobat, string keyword)
    {
        var query = new BookingSearchQuery(tglBerobat, keyword);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

}