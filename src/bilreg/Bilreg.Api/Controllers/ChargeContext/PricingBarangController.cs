using Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.ChargeContext;

/// <summary>
/// RESTful endpoint administrasi harga jual barang persediaan: matriks harga per tipe penjamin,
/// simulasi kalkulasi harga, penerbitan batch HNA, dan konfigurasi pembulatan kasir
/// (<c>OC-TRK-PRICING-POLICY-BARANG</c>).
/// </summary>
[Route("api/v1/charge/pricing-barang")]
[ApiController]
[Authorize]
public class PricingBarangController : ControllerBase
{
    private readonly IMediator _mediator;

    public PricingBarangController(IMediator mediator) => _mediator = mediator;

    /// <summary>Detail matriks harga barang di seluruh tipe penjamin.</summary>
    [HttpGet("{barangId}")]
    public async Task<IActionResult> Get(string barangId)
        => Ok(new JSendOk(await _mediator.Send(new PricingBarangGetByBarangIdQuery(barangId))));

    /// <summary>Update plafon HET dan matriks varian harga per tipe penjamin.</summary>
    [HttpPut("{barangId}")]
    public async Task<IActionResult> Update(string barangId, [FromBody] PricingBarangUpdateBody body)
    {
        var cmd = new PricingBarangUpdateCommand(
            barangId,
            body.HetNominal,
            body.MetodeGenHna,
            body.Variants,
            body.UserId);

        return Ok(new JSendOk(await _mediator.Send(cmd)));
    }

    /// <summary>Evaluasi real-time kalkulasi harga jual satuan barang.</summary>
    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] CalculateItemPriceQuery query)
        => Ok(new JSendOk(await _mediator.Send(query)));

    /// <summary>Trigger penerbitan batch HNA dari riwayat faktur DO pengadaan.</summary>
    [HttpPost("generate-hna")]
    public async Task<IActionResult> GenerateHna([FromBody] GenerateHnaBatchCommand cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd)));

    /// <summary>Konfigurasi pembulatan total tagihan kasir yang aktif.</summary>
    [HttpGet("rounding")]
    public async Task<IActionResult> GetRounding()
        => Ok(new JSendOk(await _mediator.Send(new RoundingPolicyGetQuery())));

    /// <summary>Pemutakhiran konfigurasi pembulatan total tagihan kasir.</summary>
    [HttpPut("rounding")]
    public async Task<IActionResult> UpdateRounding([FromBody] RoundingPolicyUpdateCommand cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd)));
}

/// <summary>
/// Body pemutakhiran parameter harga barang; kode barang diambil dari segmen rute
/// (<c>PUT /api/v1/charge/pricing-barang/{barangId}</c>).
/// </summary>
public record PricingBarangUpdateBody(
    decimal HetNominal,
    MetodeGenHnaEnum MetodeGenHna,
    IReadOnlyList<PricingBarangVariantInput> Variants,
    string UserId);
