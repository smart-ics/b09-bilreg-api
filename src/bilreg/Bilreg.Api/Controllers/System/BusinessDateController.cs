using Bilreg.Application.Shared.BusinessDateFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bilreg.Api.Controllers.System;

[Route("api/system")]
[ApiController]
public class BusinessDateController : ControllerBase
{
    private readonly IMediator _mediator;

    public BusinessDateController(IMediator mediator) => _mediator = mediator;

    [HttpGet("business-date")]
    public async Task<IActionResult> GetBusinessDate()
    {
        var result = await _mediator.Send(new GetBusinessDateStatusQry());
        return Ok(result);
    }
}
