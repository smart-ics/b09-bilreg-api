using Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.ChargeContext;

/// <summary>
/// RESTful endpoint administrasi master tipe harga penjamin beserta jasa kefarmasian
/// Tuslah <c>/r</c> dan Embalase <c>/R</c> (<c>OC-TRK-PRICING-POLICY-BARANG</c>).
/// </summary>
[Route("api/v1/charge/tipe-harga")]
[ApiController]
[Authorize]
public class TipeHargaController : ControllerBase
{
    private readonly IMediator _mediator;

    public TipeHargaController(IMediator mediator) => _mediator = mediator;

    /// <summary>Daftar seluruh tipe harga/penjamin.</summary>
    [HttpGet]
    public async Task<IActionResult> List()
        => Ok(new JSendOk(await _mediator.Send(new TipeHargaListQuery())));

    /// <summary>Pendaftaran/update master tipe harga penjamin.</summary>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] TipeHargaSaveCommand cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd)));
}
