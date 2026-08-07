using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;
using Bilreg.Domain.AdmisiContext.EmrAntrianOutboundFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.EmrAntrianOutboundFeature;

public class EmrAntrianOutboundQueueRepo : IEmrAntrianOutboundQueueRepo
{
    private readonly IEmrAntrianOutboundQueueDal _dal;

    public EmrAntrianOutboundQueueRepo(IEmrAntrianOutboundQueueDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(EmrAntrianOutboundQueueModel model)
    {
        var dto = EmrAntrianOutboundQueueDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(dto),
                onNone: () => _dal.Insert(dto));
    }

    public MayBe<EmrAntrianOutboundQueueModel> LoadEntity(IEmrAntrianOutboundQueueKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null ? MayBe<EmrAntrianOutboundQueueModel>.None : MayBe.From(dto.ToModel());
    }

    public IEnumerable<EmrAntrianOutboundQueueModel> ListProcessable(int batchSize)
        => _dal.ListProcessable(batchSize).Select(x => x.ToModel()).ToList();

    public MayBe<EmrAntrianOutboundQueueModel> FindActiveBySource(string sourceId, string messageType)
    {
        var dto = _dal.FindActiveBySource(sourceId, messageType);
        return dto is null ? MayBe<EmrAntrianOutboundQueueModel>.None : MayBe.From(dto.ToModel());
    }

    public void DeleteBySource(string sourceId)
    {
        _dal.DeleteBySourceId(sourceId);
    }
}
