using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;

/// <summary>
/// Handler command penyimpanan master tipe harga penjamin. Memvalidasi keunikan kode tipe harga
/// (update bila sudah terdaftar, insert bila belum) serta menolak komponen biaya negatif
/// (outcome AC-PRC-11), lalu menyimpan Tuslah <c>/r</c> dan Embalase <c>/R</c> ke
/// <c>tb_tipe_barang</c>.
/// </summary>
public class TipeHargaSaveHandler
    : IRequestHandler<TipeHargaSaveCommand, TipeHargaSaveResponse>
{
    private readonly ITipeHargaRepo _tipeHargaRepo;

    public TipeHargaSaveHandler(ITipeHargaRepo tipeHargaRepo)
    {
        _tipeHargaRepo = tipeHargaRepo;
    }

    public Task<TipeHargaSaveResponse> Handle(
        TipeHargaSaveCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TipeHargaId, nameof(request.TipeHargaId));
        Guard.Against.NullOrWhiteSpace(request.TipeHargaName, nameof(request.TipeHargaName));

        if (request.DefaultMarginPersen < 0m
            || request.BiayaPerObat_r < 0m
            || request.BiayaPerResep_R < 0m
            || request.TaxPersen < 0m
            || request.DiskonPersen < 0m)
        {
            throw new ArgumentException(
                $"Komponen biaya tipe harga '{request.TipeHargaId}' tidak boleh bernilai negatif.");
        }

        var isNew = !_tipeHargaRepo.LoadEntity(TipeHargaModel.Key(request.TipeHargaId)).HasValue;

        var model = TipeHargaModel.Create(
            request.TipeHargaId,
            request.TipeHargaName,
            request.DefaultMarginPersen,
            request.BiayaPerObat_r,
            request.BiayaPerResep_R,
            request.TaxPersen,
            request.DiskonPersen,
            request.IsActive);

        _tipeHargaRepo.SaveChanges(model);

        return Task.FromResult(new TipeHargaSaveResponse(model.TipeHargaId, isNew));
    }
}
