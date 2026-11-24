using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;

/// <summary>
/// Handler <see cref="GenerateHnaBatchCommand"/>: membaca riwayat faktur DO pengadaan, menghitung
/// nilai basis per faktur sesuai <c>Cara Generate</c>, mengagregasinya sesuai <c>Metode</c>,
/// menerbitkan nomor transaksi <c>HN...</c>, dan menyimpan batch secara atomik melalui
/// <see cref="IHnaBatchRepo"/> (atomisitas transaksi dimiliki DAL, ARCHITECTURE TD-04).
/// </summary>
public class GenerateHnaBatchHandler : IRequestHandler<GenerateHnaBatchCommand, GenerateHnaBatchResponse>
{
    private readonly IPurchaseReceiptHnaQuery _purchaseReceiptHnaQuery;
    private readonly IHnaBatchRepo _hnaBatchRepo;

    public GenerateHnaBatchHandler(
        IPurchaseReceiptHnaQuery purchaseReceiptHnaQuery,
        IHnaBatchRepo hnaBatchRepo)
    {
        _purchaseReceiptHnaQuery = purchaseReceiptHnaQuery;
        _hnaBatchRepo = hnaBatchRepo;
    }

    public Task<GenerateHnaBatchResponse> Handle(
        GenerateHnaBatchCommand request,
        CancellationToken cancellationToken)
    {
        var barangIds = (request.BarangIds ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (barangIds.Count == 0)
            throw new ArgumentException("Minimal satu barang harus ditentukan untuk penerbitan batch HNA.", nameof(request));

        if (request.TglBerlaku == default)
            throw new ArgumentException("TglBerlaku batch HNA harus diisi dengan tanggal berlaku yang valid.", nameof(request));

        var tglTrs = (request.TglTrs ?? DateTime.Now).Date;
        var batch = HnaBatchModel.Create(request.PetugasId, tglTrs);

        foreach (var barangId in barangIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var history = _purchaseReceiptHnaQuery
                .GetProcurementHistory(barangId, request.StartDate, request.EndDate)?
                .OrderBy(x => x.TglTrs)
                .ThenBy(x => x.ReceiptId, StringComparer.Ordinal)
                .ThenBy(x => x.NoUrut)
                .ToList() ?? [];

            // Outcome EX-PRC-01: riwayat pengadaan tidak ditemukan memblokir komputasi otomatis HNA.
            if (history.Count == 0)
                throw new InvalidOperationException(
                    $"Riwayat pengadaan untuk barang '{barangId}' tidak ditemukan. " +
                    "Generate HNA otomatis diblokir (EX-PRC-01); tetapkan Harga Jual Fix sementara atau isi HNA awal secara manual.");

            var basisList = history
                .Select(x => ComputeBasis(x, request.CaraGenerate))
                .ToList();

            var hnaAkhir = basisList[^1];
            var hnaMax = basisList.Max();
            var hnaRata = Math.Round(basisList.Average(), 2, MidpointRounding.AwayFromZero);

            var hnaGen = request.Metode switch
            {
                MetodeGenHnaEnum.Terakhir => hnaAkhir,
                MetodeGenHnaEnum.Terbesar => hnaMax,
                MetodeGenHnaEnum.RataRata => hnaRata,
                _ => throw new InvalidOperationException($"Metode generate HNA '{request.Metode}' tidak dikenal.")
            };

            var diskonGen = history[^1].Diskon;

            batch.AddItem(
                barangId,
                hnaAkhir,
                hnaRata,
                hnaMax,
                hnaGen,
                diskonGen,
                request.TglBerlaku);
        }

        batch.Validate();
        _hnaBatchRepo.SaveChanges(batch);

        return Task.FromResult(new GenerateHnaBatchResponse(
            HnaBatchId: batch.HnaBatchId,
            TglTrs: batch.TglTrs,
            TglBerlaku: request.TglBerlaku,
            ItemCount: batch.ListItem.Count,
            Items: batch.ListItem
                .Select(x => new GenerateHnaBatchItemResponse(
                    x.BarangId,
                    x.NoUrut,
                    x.HnaAkhir,
                    x.HnaRata,
                    x.HnaMax,
                    x.HnaGen,
                    x.DiskonGen))
                .ToList()));
    }

    /// <summary>
    /// Menghitung nilai basis HNA satu baris faktur pengadaan sesuai <c>Cara Generate</c>.
    /// Harga beli memakai nilai per satuan kecil dasar (<see cref="PurchaseReceiptHnaDto.HargaBeliPerSatuanKecil"/>)
    /// agar invariant <c>SatuanKecil</c> outcome terpenuhi. Tax dan Diskon diperlakukan sebagai nominal faktur.
    /// </summary>
    private static decimal ComputeBasis(PurchaseReceiptHnaDto receipt, CaraGenerateHnaEnum cara) => cara switch
    {
        CaraGenerateHnaEnum.Hpp => receipt.HargaBeliPerSatuanKecil,
        CaraGenerateHnaEnum.HppPlusTax => receipt.HargaBeliPerSatuanKecil + receipt.Tax,
        CaraGenerateHnaEnum.HppPlusTaxMinDisc => receipt.HargaBeliPerSatuanKecil + receipt.Tax - receipt.Diskon,
        _ => throw new InvalidOperationException($"Cara generate HNA '{cara}' tidak dikenal.")
    };
}
