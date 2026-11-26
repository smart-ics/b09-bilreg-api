using System.Transactions;
using Bilreg.Application.RadContext.RadJadwalFeature;
using Bilreg.Domain.RadContext.RadJadwalFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.RadContext.RadJadwalFeature;

public class RadJadwalRepo : IRadJadwalRepo
{
    private readonly IRadJadwalDal _jadwalDal;

    public RadJadwalRepo(IRadJadwalDal jadwalDal)
    {
        _jadwalDal = jadwalDal;
    }

    public void SaveChanges(RadJadwalModel model)
    {
        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        var dto = RadJadwalDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _jadwalDal.Update(dto),
                onNone: () => _jadwalDal.Insert(dto));

        scope.Complete();
    }

    public void SaveReschedule(RadJadwalModel model, RadJadwalRescheduleDto rescheduleHistory)
    {
        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        var dto = RadJadwalDto.FromModel(model);
        _jadwalDal.Update(dto);
        _jadwalDal.InsertReschedule(rescheduleHistory);

        scope.Complete();
    }

    public MayBe<RadJadwalModel> LoadEntity(IRadJadwalKey key)
    {
        var dto = _jadwalDal.GetData(key);
        if (dto is null)
            return MayBe<RadJadwalModel>.None;

        return MayBe.From(dto.ToModel());
    }

    public MayBe<RadJadwalModel> LoadActiveByOrderId(string orderId)
    {
        var dto = _jadwalDal.GetActiveByOrderId(orderId);
        if (dto is null)
            return MayBe<RadJadwalModel>.None;

        return MayBe.From(dto.ToModel());
    }

    public IEnumerable<RadJadwalModel> ListByOrderId(string orderId)
    {
        var dtoList = _jadwalDal.ListByOrderId(orderId);
        return dtoList?.Select(x => x.ToModel()).ToList() ?? [];
    }
}
