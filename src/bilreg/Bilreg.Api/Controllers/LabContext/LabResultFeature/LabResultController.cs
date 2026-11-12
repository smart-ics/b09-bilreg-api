using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.LabContext.LabResultFeature;

[Route("api/LabContext/LabResultFeature")]
[ApiController]
[Authorize]
public class LabResultController : ControllerBase
{
    private readonly IMediator _mediator;

    public LabResultController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("record")]
    public async Task<IActionResult> Record([FromBody] LabResultRecordRequest request)
    {
        await _mediator.Send(request.ToCommand());
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch("verify")]
    public async Task<IActionResult> Verify([FromBody] LabResultVerifyRequest request)
    {
        await _mediator.Send(request.ToCommand());
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("ingest-lis")]
    [HttpPost("ingestLis")]
    public async Task<IActionResult> IngestLis([FromBody] LabResultIngestLisRequest request)
    {
        await _mediator.Send(request.ToCommand());
        return Ok(new JSendOk("Done"));
    }

    [HttpGet("worklist/verification")]
    public async Task<IActionResult> VerificationWorklist(
        [FromQuery] string? searchTerm,
        [FromQuery] DateTime? date1,
        [FromQuery] DateTime? date2)
    {
        var response = await _mediator.Send(new LabResultVerificationWorklistQuery(searchTerm, date1, date2));
        return Ok(new JSendOk(response));
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> Get(string orderId)
    {
        var response = await _mediator.Send(new LabResultGetQuery(orderId));
        return Ok(new JSendOk(response));
    }

    [HttpPatch("amend")]
    public async Task<IActionResult> Amend([FromBody] LabResultAmendCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet("{orderId}/pdf")]
    public async Task<IActionResult> Pdf(string orderId)
    {
        var file = await _mediator.Send(new LabResultPdfQuery(orderId));
        return File(file.Content, file.ContentType, file.FileName);
    }
}

public record LabResultRecordRequest(
    string OrderId,
    string UserId,
    int ResultSource,
    IEnumerable<LabResultRecordValueDto>? Values = null,
    string CriticalNote = "",
    IEnumerable<LabResultRecordItemRequestDto>? Items = null)
{
    public LabResultRecordRequest() : this("", "", 1) { }

    public LabResultRecordRequest(LabResultRecordCmd cmd)
        : this(cmd.OrderId, cmd.UserId, cmd.ResultSource, cmd.Values, cmd.CriticalNote, cmd.Items?.Select(i => new LabResultRecordItemRequestDto(
            i.TestId, i.TestName, i.ComponentCode, i.ComponentName, i.ResultType, i.NumericValue, i.TextValue, i.OptionValue, i.NarrativeValue, i.Unit, i.ReferenceRangeText, i.Value)))
    {
    }

    public LabResultRecordCmd ToCommand()
    {
        var valuesList = Values?.ToList() ?? new List<LabResultRecordValueDto>();
        var itemsList = Items?.Select(i => new LabResultRecordItemInputDto(
            i.TestId, i.TestName, i.ComponentCode, i.ComponentName, i.ResultType, i.NumericValue, i.TextValue, i.OptionValue, i.NarrativeValue, i.Unit, i.ReferenceRangeText, i.Value)).ToList();

        if (!valuesList.Any() && itemsList != null)
        {
            valuesList = itemsList.Select(i => new LabResultRecordValueDto(
                !string.IsNullOrWhiteSpace(i.ComponentCode) ? i.ComponentCode : (i.TestId ?? string.Empty),
                i.Value ?? i.OptionValue ?? i.NarrativeValue ?? i.TextValue ?? i.NumericValue?.ToString() ?? string.Empty
            )).ToList();
        }

        return new LabResultRecordCmd(
            OrderId,
            UserId,
            ResultSource,
            valuesList,
            CriticalNote ?? string.Empty,
            itemsList);
    }

    public static implicit operator LabResultRecordRequest(LabResultRecordCmd cmd) => new(cmd);
    public static implicit operator LabResultRecordCmd(LabResultRecordRequest r) => r.ToCommand();
}

public record LabResultRecordItemRequestDto(
    string? TestId = null,
    string? TestName = null,
    string? ComponentCode = null,
    string? ComponentName = null,
    int? ResultType = null,
    decimal? NumericValue = null,
    string? TextValue = null,
    string? OptionValue = null,
    string? NarrativeValue = null,
    string? Unit = null,
    string? ReferenceRangeText = null,
    string? Value = null);

public record LabResultVerifyRequest(
    string OrderId,
    string VerifiedUserId,
    DateTime VerifiedDate,
    string ExpertiseNote = "")
{
    public LabResultVerifyRequest() : this("", "", DateTime.MinValue) { }

    public LabResultVerifyRequest(LabResultVerifyCmd cmd)
        : this(cmd.OrderId, cmd.VerifiedUserId, cmd.VerifiedDate, cmd.ExpertiseNote)
    {
    }

    public LabResultVerifyCmd ToCommand() =>
        new(OrderId, VerifiedUserId, VerifiedDate, ExpertiseNote ?? string.Empty);

    public static implicit operator LabResultVerifyRequest(LabResultVerifyCmd cmd) => new(cmd);
    public static implicit operator LabResultVerifyCmd(LabResultVerifyRequest r) => r.ToCommand();
}

public record LabResultIngestLisRequest(
    string OrderId,
    string LisRefNo,
    string ValidatorName,
    DateTime ValidatedDate,
    IEnumerable<LabResultIngestItemDto>? Items = null,
    string CriticalNote = "",
    string? OrderNo = null)
{
    public LabResultIngestLisRequest() : this("", "", "", DateTime.MinValue) { }

    public LabResultIngestLisRequest(LabResultIngestFromLisCmd cmd)
        : this(cmd.OrderId, cmd.LisRefNo, cmd.ValidatorName, cmd.ValidatedDate, cmd.Items, cmd.CriticalNote, cmd.OrderNo)
    {
    }

    public LabResultIngestFromLisCmd ToCommand()
    {
        var resolvedOrderId = !string.IsNullOrWhiteSpace(OrderId) ? OrderId : (OrderNo ?? string.Empty);
        return new LabResultIngestFromLisCmd(
            resolvedOrderId,
            LisRefNo ?? string.Empty,
            ValidatorName ?? string.Empty,
            ValidatedDate,
            Items ?? Enumerable.Empty<LabResultIngestItemDto>(),
            CriticalNote ?? string.Empty);
    }

    public static implicit operator LabResultIngestLisRequest(LabResultIngestFromLisCmd cmd) => new(cmd);
    public static implicit operator LabResultIngestFromLisCmd(LabResultIngestLisRequest r) => r.ToCommand();
}
