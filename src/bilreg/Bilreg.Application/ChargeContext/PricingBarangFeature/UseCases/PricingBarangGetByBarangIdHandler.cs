using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;

/// <summary>
/// Handler <see cref="PricingBarangGetByBarangIdQuery"/>: memuat agregat <see cref="PricingBarangModel"/>
/// dan memproyeksikannya menjadi DTO matriks harga barang di seluruh tipe penjamin.
/// Mengikuti konvensi rumah: entitas tidak ditemukan dilempar sebagai <see cref="KeyNotFoundException"/>.
/// </summary>
public class PricingBarangGetByBarangIdHandler
    : IRequestHandler<PricingBarangGetByBarangIdQuery, PricingBarangGetByBarangIdResponse>
{
    private readonly IPricingBarangRepo _pricingBarangRepo;

    public PricingBarangGetByBarangIdHandler(IPricingBarangRepo pricingBarangRepo)
    {
        _pricingBarangRepo = pricingBarangRepo;
    }

    public Task<PricingBarangGetByBarangIdResponse> Handle(
        PricingBarangGetByBarangIdQuery request,
        CancellationToken cancellationToken)
    {
        var model = _pricingBarangRepo
            .LoadEntity(PricingBarangModel.Key(request.BarangId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"Pricing barang '{request.BarangId}' tidak ditemukan."));

        var response = new PricingBarangGetByBarangIdResponse(
            BarangId: model.BarangId,
            HetNominal: model.HetNominal,
            MetodeGenHna: model.MetodeGenHna,
            Variants: model.Variants
                .Select(x => new PricingBarangVariantResponse(
                    x.TipeHargaId,
                    x.HargaJualFix,
                    x.MarginPersen,
                    x.BiayaTambahanItem,
                    x.TaxPersen,
                    x.DiskonDefaultPersen,
                    x.IsActive))
                .ToList());

        return Task.FromResult(response);
    }
}
