using Bilreg.Api.Helpers;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BedUsageContext.PakaiBedFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PakaiBedController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWardCockpitEventPublisher _eventPublisher;

    public PakaiBedController(IMediator mediator, IWardCockpitEventPublisher? eventPublisher = null)
    {
        _mediator = mediator;
        _eventPublisher = eventPublisher ?? new NullWardCockpitEventPublisher();
    }

    [HttpPost]
    [Route("Create")]
    public async Task<IActionResult> Create(PakaiBedCreateCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("release")]
    public async Task<IActionResult> Release(PakaiBedReleaseCommand cmd)
    {
        var response = await _mediator.Send(cmd);

        var targetBedId = (response.BedId ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(response.BangsalId) && !string.IsNullOrWhiteSpace(targetBedId))
        {
            await _eventPublisher.PublishBedStatusChangedAsync(response.BangsalId, targetBedId, "Cleaning");
        }

        if (response.IsBatalCheckIn)
        {
            if (!string.IsNullOrWhiteSpace(response.BangsalId))
            {
                await _eventPublisher.PublishWaitingListUpdatedAsync(response.BangsalId);
            }

            if (!string.IsNullOrWhiteSpace(response.RevertedWaitingListBangsalId) &&
                !string.Equals(response.RevertedWaitingListBangsalId.Trim(), response.BangsalId?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                await _eventPublisher.PublishWaitingListUpdatedAsync(response.RevertedWaitingListBangsalId);
            }
        }

        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("transfer")]
    [Route("/api/v1/pakai-bed/transfer")]
    [Route("/api/pakai-bed/transfer")]
    public async Task<IActionResult> Transfer([FromBody] PakaiBedTransferCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("transfer-to-waiting-list")]
    [Route("/api/v1/pakai-bed/transfer-to-waiting-list")]
    [Route("/api/pakai-bed/transfer-to-waiting-list")]
    [Route("/api/api/v1/pakai-bed/transfer-to-waiting-list")]
    public async Task<IActionResult> TransferToWaitingList([FromBody] TransferToWardWaitingListRequest req)
    {
        var effectiveUserId = !string.IsNullOrWhiteSpace(req.UserId)
            ? req.UserId
            : (User?.Identity?.Name ?? "SYSTEM");
        var cmd = new TransferToWardWaitingListCommand(
            req.PakaiBedId,
            req.TargetBangsalId,
            req.TglJamTransfer,
            req.AlasanTransfer,
            req.Catatan,
            effectiveUserId,
            req.ClinicalUrgency);
        var response = await _mediator.Send(cmd);

        if (!string.IsNullOrWhiteSpace(response.OldBangsalId) && !string.IsNullOrWhiteSpace(response.OldBedId))
        {
            await _eventPublisher.PublishBedStatusChangedAsync(response.OldBangsalId, response.OldBedId, "Cleaning");
        }

        if (!string.IsNullOrWhiteSpace(response.TargetBangsalId))
        {
            await _eventPublisher.PublishWaitingListUpdatedAsync(response.TargetBangsalId);
        }

        return Ok(new JSendOk(response));
    }

    [HttpPatch]
    [Route("batal")]
    [Route("/api/v1/pakai-bed/batal")]
    [Route("/api/pakai-bed/batal")]
    [Route("/api/api/PakaiBed/batal")]
    public async Task<IActionResult> Batal(PakaiBedVoidRequest req)
    {
        var userAgent = HttpHelper.GetUserAgent(Request);
        var remoteIpAddress = HttpHelper.GetIpAddress(Request, HttpContext);
        var cmd = new PakaiBedVoidCommand(req.PakaiBedId, req.UserId, req.VoidReason, userAgent, remoteIpAddress);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet]
    [Route("{pakaiBedId}")]
    public async Task<IActionResult> GetData(string pakaiBedId)
    {
        var query = new PakaiBedGetQuery(pakaiBedId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("{regId}/list")]
    public async Task<IActionResult> ListDataByReg(string regId)
    {
        var query = new PakaiBedListByRegQuery(regId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    public record PakaiBedVoidRequest(string PakaiBedId, string UserId, string VoidReason);

    public record TransferToWardWaitingListRequest(
        string PakaiBedId,
        string TargetBangsalId,
        DateTime TglJamTransfer,
        string AlasanTransfer,
        string? Catatan = null,
        string? UserId = null,
        string? ClinicalUrgency = null
    );
}
