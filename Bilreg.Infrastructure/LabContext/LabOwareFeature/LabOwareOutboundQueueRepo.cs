using Bilreg.Application.LabContext.LabOwareFeature;
using Bilreg.Domain.LabContext.LabOwareFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.LabContext.LabOwareFeature;

public class LabOwareOutboundQueueRepo : ILabOwareOutboundQueueRepo
{
    private readonly ILabOwareOutboundQueueDal _dal;

    public LabOwareOutboundQueueRepo(ILabOwareOutboundQueueDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(LabOwareOutboundQueueModel model)
    {
        var dto = LabOwareOutboundQueueDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(dto),
                onNone: () => _dal.Insert(dto));
    }

    public MayBe<LabOwareOutboundQueueModel> LoadEntity(ILabOwareOutboundQueueKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null ? MayBe<LabOwareOutboundQueueModel>.None : MayBe.From(dto.ToModel());
    }

    public IEnumerable<LabOwareOutboundQueueModel> ListProcessable(int batchSize)
        => _dal.ListProcessable(batchSize).Select(x => x.ToModel()).ToList();
}
