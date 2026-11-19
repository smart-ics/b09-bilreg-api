using Bilreg.Domain.PaymentContext.KasirFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public class TrsKasirRepo : ITrsKasirRepo
{
    private readonly ITrsKasirDal _trsKasirDal;

    public TrsKasirRepo(ITrsKasirDal trsKasirDal)
    {
        _trsKasirDal = trsKasirDal;
    }

    public void SaveChanges(TrsKasirModel model)
    {
        using var trans = TransHelper.NewScope();

        var existing = _trsKasirDal.GetData(model);
        var dto = TrsKasirDto.FromModel(model);
        if (existing is null)
        {
            _trsKasirDal.Insert(dto);
        }
        else
        {
            _trsKasirDal.Update(dto);
        }

        var paymentDtos = model.Payments
            .Select(p => TrsKasirPaymentDto.FromModel(p, model.TrsKasirId))
            .ToList();

        _trsKasirDal.DeletePayments(model);
        if (paymentDtos.Count > 0)
        {
            _trsKasirDal.InsertPayments(paymentDtos);
        }

        trans.Complete();
    }

    public void Save(TrsKasirModel model) => SaveChanges(model);

    public MayBe<TrsKasirModel> LoadEntity(ITrsKasirKey key)
    {
        var dto = _trsKasirDal.GetData(key);
        if (dto is null)
            return MayBe<TrsKasirModel>.None;

        var paymentDtos = _trsKasirDal.ListPayments(key)?.ToList() ?? [];
        var payments = paymentDtos.Select(p => p.ToModel()).ToList();
        return MayBe.From(dto.ToModel(payments));
    }

    public TrsKasirModel? GetKey(ITrsKasirKey key)
    {
        return LoadEntity(key).Match(x => x, () => (TrsKasirModel?)null);
    }

    public TrsKasirModel? GetKey(string trsKasirId)
    {
        return GetKey(new TrsKasirKey(trsKasirId));
    }

    public IEnumerable<TrsKasirModel> ListByShift(IKasirShiftKey shiftKey)
    {
        return ListByShift(shiftKey.ShiftId);
    }

    public IEnumerable<TrsKasirModel> ListByShift(string shiftId)
    {
        var listDto = _trsKasirDal.ListByShift(shiftId)?.ToList() ?? [];
        return MapListToModels(listDto);
    }

    public IEnumerable<TrsKasirModel> ListByReg(string regId)
    {
        var listDto = _trsKasirDal.ListByReg(regId)?.ToList() ?? [];
        return MapListToModels(listDto);
    }

    public IEnumerable<TrsKasirModel> ListByTgl(string tglTrs)
    {
        var listDto = _trsKasirDal.ListByTgl(tglTrs)?.ToList() ?? [];
        return MapListToModels(listDto);
    }

    private IEnumerable<TrsKasirModel> MapListToModels(IEnumerable<TrsKasirDto> listDto)
    {
        var result = new List<TrsKasirModel>();
        foreach (var dto in listDto)
        {
            var paymentDtos = _trsKasirDal.ListPayments(new TrsKasirKey(dto.TrsKasirId))?.ToList() ?? [];
            var payments = paymentDtos.Select(p => p.ToModel()).ToList();
            result.Add(dto.ToModel(payments));
        }
        return result;
    }
}
