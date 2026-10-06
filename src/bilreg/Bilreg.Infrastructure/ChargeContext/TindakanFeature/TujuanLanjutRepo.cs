using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

//  M03-F01 P2-S04 — read-only mapping repository (TD-04).
//  Loads all rows; the application builds TujuanLanjutService over them
//  so resolution is data-driven with no per-order-type branching.
public class TujuanLanjutRepo : ITujuanLanjutRepo
{
    private readonly ITujuanLanjutDal _tujuanLanjutDal;

    public TujuanLanjutRepo(ITujuanLanjutDal tujuanLanjutDal)
    {
        _tujuanLanjutDal = tujuanLanjutDal;
    }

    public MayBe<TujuanLanjutType> LoadEntity(ITujuanLanjutKey key)
    {
        var dto = _tujuanLanjutDal.ListData()
            ?.FirstOrDefault(x => string.Equals(x.OrderType, key.OrderType,
                StringComparison.OrdinalIgnoreCase));
        if (dto is null)
            return MayBe<TujuanLanjutType>.None;

        return MayBe.From(dto.ToModel());
    }

    public IEnumerable<TujuanLanjutType> ListData()
    {
        var listDto = _tujuanLanjutDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }
}
