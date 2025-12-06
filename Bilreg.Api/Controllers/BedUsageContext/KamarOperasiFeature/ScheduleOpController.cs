using Microsoft.AspNetCore.Mvc;

namespace Bilreg.Api.Controllers.BedUsageContext.KamarOperasiFeature;

[Route("api/[controller]")]
[ApiController]
public class ScheduleOpController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}