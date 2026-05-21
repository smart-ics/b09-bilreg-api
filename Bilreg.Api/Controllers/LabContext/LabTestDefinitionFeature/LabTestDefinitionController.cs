using Bilreg.Application.LabContext.LabTestDefinitionFeature;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.LabContext.LabTestDefinitionFeature;

[Route("api/LabContext/LabTestDefinitionFeature")]
[ApiController]
public class LabTestDefinitionController : ControllerBase
{
    private readonly IMediator _mediator;

    public LabTestDefinitionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("definitions")]
    public async Task<IActionResult> ListDefinitions(
        [FromQuery] bool activeOnly = false,
        [FromQuery] string? search = null,
        [FromQuery] string? tarifId = null)
    {
        var response = await _mediator.Send(new LabTestDefinitionListQuery(activeOnly, search, tarifId));
        return Ok(new JSendOk(response));
    }

    [HttpGet("definitions/{testDefinitionId}")]
    public async Task<IActionResult> GetDefinition(string testDefinitionId)
    {
        var response = await _mediator.Send(new LabTestDefinitionGetQuery(testDefinitionId));
        return Ok(new JSendOk(response));
    }

    [HttpGet("byTarif/{tarifId}")]
    public async Task<IActionResult> GetByTarif(string tarifId)
    {
        var response = await _mediator.Send(new LabTestDefinitionGetByTarifQuery(tarifId));
        return Ok(new JSendOk(response));
    }

    [HttpPost("definitions")]
    public async Task<IActionResult> CreateDefinition(LabTestDefinitionCreateCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPut("definitions/{testDefinitionId}")]
    public async Task<IActionResult> UpdateDefinition(
        string testDefinitionId,
        LabTestDefinitionUpdateBody body)
    {
        var cmd = new LabTestDefinitionUpdateCmd(
            testDefinitionId,
            body.TarifId,
            body.TarifCode,
            body.TarifName,
            body.LabTestCode,
            body.LabTestName,
            body.SpecimenType,
            body.VacutainerType,
            body.IsActive,
            body.UserId,
            body.Components);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("definitions/{testDefinitionId}/activate")]
    public async Task<IActionResult> ActivateDefinition(
        string testDefinitionId,
        LabTestDefinitionStatusCmd body)
    {
        await _mediator.Send(new LabTestDefinitionActivateCmd(testDefinitionId, body.UserId));
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("definitions/{testDefinitionId}/deactivate")]
    public async Task<IActionResult> DeactivateDefinition(
        string testDefinitionId,
        LabTestDefinitionStatusCmd body)
    {
        await _mediator.Send(new LabTestDefinitionDeactivateCmd(testDefinitionId, body.UserId));
        return Ok(new JSendOk("Done"));
    }
}

public record LabTestDefinitionUpdateBody(
    string TarifId,
    string TarifCode,
    string TarifName,
    string LabTestCode,
    string LabTestName,
    string SpecimenType,
    Domain.LabContext.LabOrderFeature.VacutainerTypeEnum VacutainerType,
    bool IsActive,
    string UserId,
    IReadOnlyList<LabTestComponentInput> Components);

public record LabTestDefinitionStatusCmd(string UserId);
