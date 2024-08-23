using Microsoft.AspNetCore.Mvc;

namespace Bilreg.Api.Controllers.AdmPasienContext;

public class KecamatanController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}