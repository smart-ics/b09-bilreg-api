using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;

namespace Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;

internal static class DeliveryOrderDraftReferenceValidator
{
    public static void Validate(
        string brgId,
        string satuanId,
        string layananId,
        IBrgRepo brgRepo,
        ISatuanRepo satuanRepo,
        ILayananRepo layananRepo)
    {
        var brg = brgRepo.LoadEntity(new BrgKey(brgId));
        if (!brg.HasValue)
            throw new KeyNotFoundException($"BrgId '{brgId}' tidak ditemukan");

        if (!satuanRepo.LoadEntity(SatuanType.Key(satuanId)).HasValue)
            throw new KeyNotFoundException($"SatuanId '{satuanId}' tidak ditemukan");

        if (!brg.Value.ListSatuan.Any(x => x.Satuan.SatuanId == satuanId))
            throw new InvalidOperationException(
                $"SatuanId '{satuanId}' tidak terdaftar untuk BrgId '{brgId}'");

        if (!layananRepo.LoadEntity(LayananType.Key(layananId)).HasValue)
            throw new KeyNotFoundException($"LayananId '{layananId}' tidak ditemukan");
    }

    private sealed record BrgKey(string BrgId) : IBrgKey;
}
