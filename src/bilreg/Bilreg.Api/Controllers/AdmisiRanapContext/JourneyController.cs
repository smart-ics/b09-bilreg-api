using System.Diagnostics;
using Bilreg.Api.Filters;
using Bilreg.Application.AdmisiRanapContext.JourneyFeature;
using Bilreg.Application.AdmisiRanapContext.JourneyFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiRanapContext;

[Route("api/admisi-ranap/journeys")]
[ApiController]
[Authorize]
[ServiceFilter(typeof(AdmisiRanapEnabledFilter))]
[ServiceFilter(typeof(JourneyEndpointsEnabledFilter))]
public sealed class JourneyController : ControllerBase
{
    private const int MaxPageSize = 200;
    private const int MaxSearchLength = 200;
    private const int MaxIdentifierLength = 100;
    private readonly IMediator _mediator;

    public JourneyController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? scope,
        [FromQuery] string? stage,
        [FromQuery(Name = "search")] string? search,
        [FromQuery] string? dokterId,
        [FromQuery] string? bangsalId,
        [FromQuery] string? kelasId,
        [FromQuery] string? tipeJaminanId,
        [FromQuery] int? priority,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize)
    {
        if (!TryBuildListQuery(scope, stage, search, dokterId, bangsalId, kelasId, tipeJaminanId,
                priority, dateFrom, dateTo, cursor, pageSize, out var query, out var error))
            return BadRequest(new JSend(StatusCodes.Status400BadRequest, "Bad Request", error));

        var stopwatch = Stopwatch.StartNew();
        var result = await _mediator.Send(query!);
        stopwatch.Stop();
        Response.Headers.Append("Server-Timing", $"journey;dur={stopwatch.Elapsed.TotalMilliseconds:F0}");
        return Ok(new JSendOk(result));
    }

    [HttpGet("resolve")]
    public async Task<IActionResult> Resolve([FromQuery] string? recordType, [FromQuery] string? recordId)
    {
        if (!TryLegacyInput(recordType, recordId, out var type, out var id, out var error))
            return BadRequest(new JSend(StatusCodes.Status400BadRequest, "Bad Request", error));

        var resolution = await _mediator.Send(new AdmResolveJourneyLegacyRecordQry(type!, id!));
        if (resolution is null)
            return NotFound(new JSend(StatusCodes.Status404NotFound, "Not Found", "Legacy record was not found."));
        if (resolution.RequiresReconciliation)
            return Conflict(new JSendOk(resolution));

        return Ok(new JSendOk(resolution));
    }

    [HttpGet("{journeyId}")]
    public async Task<IActionResult> Get(string journeyId)
    {
        if (!TryJourneyId(journeyId, out var normalized, out var error))
            return BadRequest(new JSend(StatusCodes.Status400BadRequest, "Bad Request", error));

        var stopwatch = Stopwatch.StartNew();
        var result = await _mediator.Send(new AdmGetJourneyQry(normalized!));
        stopwatch.Stop();
        Response.Headers.Append("Server-Timing", $"journey;dur={stopwatch.Elapsed.TotalMilliseconds:F0}");
        return result is null
            ? NotFound(new JSend(StatusCodes.Status404NotFound, "Not Found", "Journey was not found."))
            : Ok(new JSendOk(result));
    }

    private static bool TryBuildListQuery(
        string? scopeText, string? stageText, string? search, string? dokterId, string? bangsalId,
        string? kelasId, string? tipeJaminanId, int? priority, string? dateFromText, string? dateToText,
        string? cursor, int? pageSize, out AdmListJourneyQry? query, out string error)
    {
        query = null;
        if (!TryScope(scopeText, out var scope) || !TryStage(stageText, out var stage))
            return Invalid("scope or stage is unsupported.", out error);
        if (!TryDate(dateFromText, out var dateFrom) || !TryDate(dateToText, out var dateTo)
            || dateFrom > dateTo)
            return Invalid("dateFrom and dateTo must be valid ISO-8601 values with dateFrom not after dateTo.", out error);
        if (pageSize is <= 0 or > MaxPageSize)
            return Invalid($"pageSize must be between 1 and {MaxPageSize}.", out error);
        if (!string.IsNullOrWhiteSpace(cursor) && !JourneyListCursor.TryDecode(cursor, out _, out _))
            return Invalid("cursor has an invalid format.", out error);
        if (priority is < 0 || !HasValidLength(search, MaxSearchLength)
            || !HasValidLength(dokterId, MaxIdentifierLength) || !HasValidLength(bangsalId, MaxIdentifierLength)
            || !HasValidLength(kelasId, MaxIdentifierLength) || !HasValidLength(tipeJaminanId, MaxIdentifierLength))
            return Invalid("Filter value is invalid or too long.", out error);
        if (scope == JourneyListScope.Active && stage is JourneyOperationalStage.Completed or JourneyOperationalStage.Cancelled)
            return Invalid("Terminal stages require scope=history.", out error);

        query = new AdmListJourneyQry(scope, stage, Trim(search), Trim(dokterId), Trim(bangsalId), Trim(kelasId),
            Trim(tipeJaminanId), priority, dateFrom, dateTo, Trim(cursor), pageSize ?? 50);
        error = string.Empty;
        return true;
    }

    private static bool TryScope(string? text, out JourneyListScope scope)
    {
        scope = JourneyListScope.Active;
        if (string.IsNullOrWhiteSpace(text)) return true;
        return text.Trim().ToLowerInvariant() switch
        {
            "active" => true,
            "history" => SetHistory(out scope),
            _ => false
        };
    }

    private static bool SetHistory(out JourneyListScope scope) { scope = JourneyListScope.History; return true; }

    private static bool TryStage(string? text, out JourneyOperationalStage? stage)
    {
        stage = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (!Enum.TryParse<JourneyOperationalStage>(text, true, out var parsed)
            || !Enum.IsDefined(parsed) || parsed == JourneyOperationalStage.InWard)
            return false;
        stage = parsed;
        return true;
    }

    private static bool TryDate(string? text, out DateTime? date)
    {
        date = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (!DateTimeOffset.TryParse(text, out var parsed)) return false;
        date = parsed.UtcDateTime;
        return true;
    }

    private static bool TryLegacyInput(string? recordType, string? recordId, out string? type, out string? id, out string error)
    {
        type = recordType?.Trim().ToLowerInvariant(); id = recordId?.Trim();
        if (string.IsNullOrWhiteSpace(id) || id.Length > MaxIdentifierLength)
            return Invalid("recordId is required and must be at most 100 characters.", out error);
        if (type is not ("opnamerequest" or "opname" or "opn" or "reservation" or "rsv" or "admission" or "registration" or "reg" or "waitinglist" or "wl" or "wtl"))
            return Invalid("recordType is unsupported.", out error);
        error = string.Empty; return true;
    }

    private static bool TryJourneyId(string? journeyId, out string? normalized, out string error)
    {
        normalized = journeyId?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > MaxIdentifierLength
            || !JourneyIdFactory.TryParse(normalized, out _, out _))
            return Invalid("journeyId is invalid.", out error);
        error = string.Empty; return true;
    }

    private static bool HasValidLength(string? value, int max) => value is null || value.Trim().Length <= max;
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool Invalid(string message, out string error) { error = message; return false; }
}
