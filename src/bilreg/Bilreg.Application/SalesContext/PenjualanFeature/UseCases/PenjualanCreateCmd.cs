using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using MediatR;
using SalesTipeJaminanReff = Bilreg.Domain.SalesContext.Shared.TipeJaminanReff;

namespace Bilreg.Application.SalesContext.PenjualanFeature.UseCases;

public record PenjualanCreateCmd(
    string ResepId,
    string LayananId,
    string TipeJaminanId,
    string UserId
) : IRequest<PenjualanCreateResponse>, IResepKey, ILayananKey, ITipeJaminanKey;

public record PenjualanCreateResponse(string PenjualanId);

public class PenjualanCreateHandler : IRequestHandler<PenjualanCreateCmd, PenjualanCreateResponse>
{
    private readonly IPenjualanRepo _penjualanRepo;
    private readonly IResepRepo _resepRepo;
    private readonly InventoryContext.StokFeature.ILayananRepo _layananRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;

    public PenjualanCreateHandler(
        IPenjualanRepo penjualanRepo,
        IResepRepo resepRepo,
        InventoryContext.StokFeature.ILayananRepo layananRepo,
        ITipeJaminanRepo tipeJaminanRepo)
    {
        _penjualanRepo = penjualanRepo;
        _resepRepo = resepRepo;
        _layananRepo = layananRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
    }

    public Task<PenjualanCreateResponse> Handle(
        PenjualanCreateCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.ResepId);
        Guard.Against.NullOrWhiteSpace(request.LayananId);
        Guard.Against.NullOrWhiteSpace(request.TipeJaminanId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var resep = _resepRepo.LoadEntity(request)
            .GetValueOrThrow($"Resep '{request.ResepId}' tidak ditemukan.");

        if (resep.AuditTrail.IsVoided)
            throw new InvalidOperationException($"Resep '{request.ResepId}' sudah void.");

        if (!resep.ListObat.Any())
            throw new InvalidOperationException($"Resep '{request.ResepId}' tidak memiliki item obat.");

        var layanan = _layananRepo.LoadEntity(request)
            .GetValueOrThrow($"Layanan '{request.LayananId}' tidak ditemukan.");

        var tipeJaminan = _tipeJaminanRepo.LoadEntity(request)
            .GetValueOrThrow($"Tipe Jaminan '{request.TipeJaminanId}' tidak ditemukan.");

        var tipeJaminanReff = new SalesTipeJaminanReff(
            tipeJaminan.TipeJaminanId,
            tipeJaminan.TipeJaminanName);

        var penjualan = PenjualanModel.CreateFromResep(
            resep,
            tipeJaminanReff,
            layanan,
            request.UserId);

        _penjualanRepo.SaveChanges(penjualan);

        return Task.FromResult(new PenjualanCreateResponse(penjualan.PenjualanId));
    }
}
