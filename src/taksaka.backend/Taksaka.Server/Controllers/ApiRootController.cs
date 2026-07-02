using Microsoft.AspNetCore.Mvc;
using Taksaka.Infrastructure.SignalR;

namespace Taksaka.Server.Controllers;

[ApiController]
[Route("/")]
public sealed class ApiRootController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            service = "Taksaka API",
            health = "/health",
            signalR = SignalREndpoints.Operations
        });
    }
}
