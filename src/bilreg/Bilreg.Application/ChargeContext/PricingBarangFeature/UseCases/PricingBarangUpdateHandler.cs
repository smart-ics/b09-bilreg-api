using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;

/// <summary>
/// Handler command pemutakhiran parameter harga jual barang persediaan. Memvalidasi keberadaan
/// <c>BarangId</c> dan nilai HET non-negatif, melakukan upsert agregat <see cref="PricingBarangModel"/>
/// (header <c>tb_barang</c> + matriks varian <c>tb_barang_tipe</c>), dan mencatat jejak audit
/// (dibuat vs diubah).
/// </summary>
public class PricingBarangUpdateHandler
    : IRequestHandler<PricingBarangUpdateCommand, PricingBarangUpdateResponse>
{
    private readonly IPricingBarangRepo _pricingBarangRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public PricingBarangUpdateHandler(
        IPricingBarangRepo pricingBarangRepo,
        ITglJamProvider tglJamProvider)
    {
        _pricingBarangRepo = pricingBarangRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<PricingBarangUpdateResponse> Handle(
        PricingBarangUpdateCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BarangId, nameof(request.BarangId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        if (request.HetNominal < 0m)
        {
            throw new ArgumentException(
                $"Nilai HET tidak boleh negatif untuk barang '{request.BarangId}' " +
                $"(diterima: {request.HetNominal}).",
                nameof(request.HetNominal));
        }

        var now = _tglJamProvider.Now;
        var existing = _pricingBarangRepo.LoadEntity(PricingBarangModel.Key(request.BarangId));
        var isNew = !existing.HasValue;

        var model = existing.Match(
            onSome: current =>
            {
                current.AuditTrail.Modif(request.UserId, now);
                return current with
                {
                    HetNominal = request.HetNominal,
                    MetodeGenHna = request.MetodeGenHna
                };
            },
            onNone: () => PricingBarangModel.Create(
                request.BarangId,
                request.HetNominal,
                request.MetodeGenHna,
                request.UserId,
                now));

        foreach (var variant in request.Variants ?? [])
        {
            Guard.Against.NullOrWhiteSpace(variant.TipeHargaId, nameof(variant.TipeHargaId));

            model = model.AddOrUpdateVariant(
                variant.TipeHargaId,
                variant.HargaJualFix,
                variant.MarginPersen,
                variant.BiayaTambahanItem,
                variant.TaxPersen,
                variant.DiskonDefaultPersen,
                variant.IsActive);
        }

        _pricingBarangRepo.SaveChanges(model);

        return Task.FromResult(new PricingBarangUpdateResponse(
            model.BarangId,
            isNew,
            model.Variants.Count,
            model.HetNominal));
    }
}
