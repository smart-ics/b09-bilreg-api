using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public class TarifPublishLogRepo : ITarifPublishLogRepo
{
    private readonly ITarifPublishLogDal _tarifPublishLogDal;
    private readonly ITarifPublishLogDetailDal _tarifPublishLogDetailDal;

    public TarifPublishLogRepo(
        ITarifPublishLogDal tarifPublishLogDal,
        ITarifPublishLogDetailDal tarifPublishLogDetailDal)
    {
        _tarifPublishLogDal = tarifPublishLogDal;
        _tarifPublishLogDetailDal = tarifPublishLogDetailDal;
    }

    public void Insert(TarifPublishLogType log)
    {
        _tarifPublishLogDal.Insert(TarifPublishLogDto.FromModel(log));

        var detailDtos = log.Details
            .Select(d => TarifPublishLogDetailDto.FromModel(log.PublishLogId, d))
            .ToList();
        if (detailDtos.Count > 0)
            _tarifPublishLogDetailDal.Insert(detailDtos);
    }

    public MayBe<TarifPublishLogType> LoadEntity(ITarifPublishLogKey key)
    {
        var headerDto = _tarifPublishLogDal.GetData(key);
        if (headerDto is null)
            return MayBe<TarifPublishLogType>.None;

        var detailDtos = _tarifPublishLogDetailDal.ListData(key)?.ToList() ?? [];
        var details = detailDtos.Select(x => x.ToModel());
        return MayBe.From(headerDto.ToModel(details));
    }

    public IEnumerable<TarifPublishLogType> ListByPolicy(ITarifPolicyKey policyKey)
    {
        var headers = _tarifPublishLogDal.ListData(policyKey)?.ToList() ?? [];
        return headers.Select(header =>
        {
            var logKey = TarifPublishLogType.Key(header.PublishLogId);
            var detailDtos = _tarifPublishLogDetailDal.ListData(logKey)?.ToList() ?? [];
            var details = detailDtos.Select(x => x.ToModel());
            return header.ToModel(details);
        });
    }
}
