using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Bilreg.Api.Controllers.AdmPasienContext;

public class KabupatenController : Controller
{
    private readonly IMediator _mediator;

    public KabupatenController(IMediator mediator)
    {
        _mediator = mediator;
    }
}