using MediatR;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;

/// <summary>
/// Handler <see cref="TipeHargaListQuery"/>: membaca seluruh master tipe harga penjamin dan
/// memproyeksikannya menjadi daftar DTO.
/// </summary>
public class TipeHargaListHandler : IRequestHandler<TipeHargaListQuery, IReadOnlyList<TipeHargaListResponse>>
{
    private readonly ITipeHargaRepo _tipeHargaRepo;

    public TipeHargaListHandler(ITipeHargaRepo tipeHargaRepo)
    {
        _tipeHargaRepo = tipeHargaRepo;
    }

    public Task<IReadOnlyList<TipeHargaListResponse>> Handle(
        TipeHargaListQuery request,
        CancellationToken cancellationToken)
    {
        var list = _tipeHargaRepo.ListData()?.ToList() ?? [];

        IReadOnlyList<TipeHargaListResponse> response = list
            .OrderBy(x => x.TipeHargaId, StringComparer.Ordinal)
            .Select(x => new TipeHargaListResponse(
                x.TipeHargaId,
                x.TipeHargaName,
                x.DefaultMarginPersen,
                x.BiayaPerObat_r,
                x.BiayaPerResep_R,
                x.TaxPersen,
                x.DiskonPersen,
                x.IsActive))
            .ToList();

        return Task.FromResult(response);
    }
}
