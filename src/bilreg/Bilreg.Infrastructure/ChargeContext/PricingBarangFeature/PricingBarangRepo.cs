using Bilreg.Application.ChargeContext.PricingBarangFeature;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

public class PricingBarangRepo : IPricingBarangRepo
{
    private readonly IPricingBarangDal _barangDal;
    private readonly IPricingBarangItemDal _itemDal;

    public PricingBarangRepo(IPricingBarangDal barangDal, IPricingBarangItemDal itemDal)
    {
        _barangDal = barangDal;
        _itemDal = itemDal;
    }

    public void SaveChanges(PricingBarangModel model)
    {
        _barangDal.UpdateHetAndMetode(model.BarangId, model.HetNominal, model.MetodeGenHna);

        var items = model.Variants
            .Select(x => ItemPricingVariantDto.FromModel(model.BarangId, x))
            .ToList();

        _itemDal.SaveBatch(model.BarangId, items);
    }

    public MayBe<PricingBarangModel> LoadEntity(IPricingBarangKey key)
    {
        var barangDto = _barangDal.GetData(key);
        if (barangDto is null)
            return MayBe<PricingBarangModel>.None;

        var model = barangDto.ToModel();
        var variants = _itemDal.ListData(key)?.ToList() ?? [];

        foreach (var dto in variants)
            model = ApplyVariant(model, dto);

        return MayBe.From(model);
    }

    public void DeleteEntity(IPricingBarangKey key)
    {
        _itemDal.Delete(key);
    }

    public IEnumerable<PricingBarangModel> ListData()
    {
        var allItems = _itemDal.ListData()?.ToList() ?? [];
        var result = new List<PricingBarangModel>();

        foreach (var group in allItems.GroupBy(x => x.fs_kd_barang))
        {
            var barangDto = _barangDal.GetData(PricingBarangModel.Key(group.Key));
            if (barangDto is null)
                continue;

            var model = barangDto.ToModel();
            foreach (var dto in group)
                model = ApplyVariant(model, dto);

            result.Add(model);
        }

        return result;
    }

    private static PricingBarangModel ApplyVariant(PricingBarangModel model, ItemPricingVariantDto dto)
    {
        var variant = dto.ToModel();
        return model.AddOrUpdateVariant(
            variant.TipeHargaId,
            variant.HargaJualFix,
            variant.MarginPersen,
            variant.BiayaTambahanItem,
            variant.TaxPersen,
            variant.DiskonDefaultPersen,
            variant.IsActive);
    }
}
