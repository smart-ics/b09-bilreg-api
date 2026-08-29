using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.SalesContext.PenjualanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.ReturJualFeature;
using MediatR;

namespace Bilreg.Application.SalesContext.ReturJualFeature.UseCases;

public record ReturJualCreateCmd(
    string PenjualanId, string LayananId, string Reason, string UserId,
    IEnumerable<ReturJualCreateItemCmd> ListObat)
    : IRequest<ReturJualCreateResponse>, IPenjualanKey, ILayananKey;

public record ReturJualCreateItemCmd(string BrgId, decimal QtyRetur, decimal HargaRetur);

public record ReturJualCreateResponse(string ReturJualId);

public class ReturJualCreateHandler
    : IRequestHandler<ReturJualCreateCmd, ReturJualCreateResponse>
{
    private readonly IReturJualRepo _returJualRepo;
    private readonly IPenjualanRepo _penjualanRepo;
    private readonly ILayananRepo _layananRepo;

    public ReturJualCreateHandler(IReturJualRepo returJualRepo,
        IPenjualanRepo penjualanRepo, ILayananRepo layananRepo)
    {
        _returJualRepo = returJualRepo;
        _penjualanRepo = penjualanRepo;
        _layananRepo = layananRepo;
    }

    public Task<ReturJualCreateResponse> Handle(ReturJualCreateCmd request, 
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PenjualanId);
        Guard.Against.NullOrWhiteSpace(request.LayananId);
        Guard.Against.NullOrWhiteSpace(request.Reason);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        Guard.Against.Null(request.ListObat);

        var listObat = request.ListObat.ToList();
        if (listObat.Count == 0)
            throw new ArgumentException("Minimal harus ada 1 item retur.");

        var penjualan = LoadPenjualan(request);
        var layanan = _layananRepo.LoadEntity(request)
            .GetValueOrThrow($"Layanan '{request.LayananId}' tidak ditemukan.");

        var retur = ReturJualModel.Create(penjualan, layanan, request.Reason, request.UserId);
        BuildItemRetur(retur, penjualan, listObat);
        retur.SetPembulatan(1); // harusnya ada setting nilai pembulatan

        _returJualRepo.SaveChanges(retur);

        return Task.FromResult(new ReturJualCreateResponse(retur.ReturJualId));
    }

    private ReturJualModel BuildItemRetur(ReturJualModel retur, PenjualanModel penjualan, 
        IEnumerable<ReturJualCreateItemCmd> listObat)
    {
        var listBrgReturBefore = _returJualRepo.ListReturQtyByPenjualan(penjualan, retur);

        foreach (var obat in listObat)
        {
            var jualItem = penjualan.ListItem.FirstOrDefault(x => x.Brg.BrgId == obat.BrgId) 
                ?? throw new ArgumentException($"Brg '{obat.BrgId}' tidak ada di Penjualan");

            var qtySudahDiretur = listBrgReturBefore
                .Where(x => x.BrgId == jualItem.Brg.BrgId)
                .Sum(x => x.QtyRetur);

            var qtySisaRetur = jualItem.Qty - qtySudahDiretur;
            if (qtySisaRetur <= 0)
                 throw new ArgumentException($"Brg '{obat.BrgId}' sudah tidak memiliki qty yang dapat diretur.");

            var item = new ReturableItemType(
                jualItem.Brg, jualItem.Satuan, jualItem.Qty, jualItem.Nilai.Harga, qtySisaRetur);
            var taxPerUnit = jualItem.Nilai.Tax / jualItem.Qty;
            retur.AddItem(item, obat.QtyRetur, obat.HargaRetur, taxPerUnit);
        }

        return retur;
    }

    private PenjualanModel LoadPenjualan(ReturJualCreateCmd request)
    {
        var penjualan = _penjualanRepo.LoadEntity(request)
            .GetValueOrThrow($"Penjualan '{request.PenjualanId}' tidak ditemukan.");

        if (penjualan.AuditTrail.IsVoided)
            throw new InvalidOperationException($"Penjualan '{request.PenjualanId}' sudah void.");
        if (!penjualan.ListItem.Any())
            throw new InvalidOperationException($"Penjualan '{request.PenjualanId}' tidak memiliki item obat.");

        return penjualan;
    }
}