using Bilreg.Application.ChargeContext.PricingBarangFeature;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

public class TipeHargaRepo : ITipeHargaRepo
{
    private readonly ITipeHargaDal _tipeHargaDal;

    public TipeHargaRepo(ITipeHargaDal tipeHargaDal)
    {
        _tipeHargaDal = tipeHargaDal;
    }

    public void SaveChanges(TipeHargaModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _tipeHargaDal.Update(TipeHargaDto.FromModel(model)),
                onNone: () => _tipeHargaDal.Insert(TipeHargaDto.FromModel(model)));
    }

    public MayBe<TipeHargaModel> LoadEntity(ITipeHargaKey key)
    {
        var dto = _tipeHargaDal.GetData(key);
        if (dto is null)
            return MayBe<TipeHargaModel>.None;

        return MayBe.From(dto.ToModel());
    }

    public void DeleteEntity(ITipeHargaKey key)
    {
        _tipeHargaDal.Delete(key);
    }

    public IEnumerable<TipeHargaModel> ListData()
    {
        var listDto = _tipeHargaDal.ListData()?.ToList() ?? [];
        return listDto.Select(x => x.ToModel()).ToList();
    }
}
