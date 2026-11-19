using Bilreg.Domain.PaymentContext.KasirFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public class KasirShiftRepo : IKasirShiftRepo
{
    private readonly IKasirShiftDal _kasirShiftDal;

    public KasirShiftRepo(IKasirShiftDal kasirShiftDal)
    {
        _kasirShiftDal = kasirShiftDal;
    }

    public void SaveChanges(KasirShiftModel model)
    {
        var existing = _kasirShiftDal.GetData(model);
        var dto = KasirShiftDto.FromModel(model);
        if (existing is null)
        {
            _kasirShiftDal.Insert(dto);
        }
        else
        {
            _kasirShiftDal.Update(dto);
        }
    }

    public void Save(KasirShiftModel model) => SaveChanges(model);

    public MayBe<KasirShiftModel> LoadEntity(IKasirShiftKey key)
    {
        var dto = _kasirShiftDal.GetData(key);
        if (dto is null)
            return MayBe<KasirShiftModel>.None;

        return MayBe.From(dto.ToModel());
    }

    public KasirShiftModel? GetKey(IKasirShiftKey key)
    {
        return LoadEntity(key).Match(x => x, () => (KasirShiftModel?)null);
    }

    public KasirShiftModel? GetKey(string shiftId)
    {
        return GetKey(new KasirShiftKey(shiftId));
    }

    public KasirShiftModel? GetActiveShift(string kasirId)
    {
        var dto = _kasirShiftDal.GetActiveShift(kasirId);
        return dto?.ToModel();
    }

    public IEnumerable<KasirShiftModel> ListByTgl(string tglShift)
    {
        var listDto = _kasirShiftDal.ListByTgl(tglShift)?.ToList() ?? [];
        return listDto.Select(x => x.ToModel());
    }
}
