using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfListTarifPolicyPublishLogQry(string TarifPolicyId)
    : IRequest<IReadOnlyList<TrfTarifPolicyPublishLogResponse>>, ITarifPolicyKey;

public record TrfTarifPolicyPublishLogResponse(
    string PublishLogId,
    string TarifPolicyId,
    string PublishedBy,
    DateTime PublishedDate,
    int VariantCount,
    string Note,
    IReadOnlyList<TrfTarifPolicyPublishLogDetailResponse> Details);

public record TrfTarifPolicyPublishLogDetailResponse(
    int ItemNo,
    string TarifId,
    string KelasId,
    string TipeTarifId,
    string NilaiTarifId,
    decimal Nilai);

public class TrfListTarifPolicyPublishLogHandler
    : IRequestHandler<TrfListTarifPolicyPublishLogQry, IReadOnlyList<TrfTarifPolicyPublishLogResponse>>
{
    private readonly ITarifPolicyRepo _tarifPolicyRepo;
    private readonly ITarifPublishLogRepo _tarifPublishLogRepo;

    public TrfListTarifPolicyPublishLogHandler(
        ITarifPolicyRepo tarifPolicyRepo,
        ITarifPublishLogRepo tarifPublishLogRepo)
    {
        _tarifPolicyRepo = tarifPolicyRepo;
        _tarifPublishLogRepo = tarifPublishLogRepo;
    }

    public Task<IReadOnlyList<TrfTarifPolicyPublishLogResponse>> Handle(
        TrfListTarifPolicyPublishLogQry request,
        CancellationToken cancellationToken)
    {
        _ = TrfTarifPolicySupport.LoadPolicy(_tarifPolicyRepo, request);

        var logs = _tarifPublishLogRepo.ListByPolicy(request)
            .OrderByDescending(l => l.PublishedDate)
            .Select(l => new TrfTarifPolicyPublishLogResponse(
                l.PublishLogId,
                l.TarifPolicyId,
                l.PublishedBy,
                l.PublishedDate,
                l.VariantCount,
                l.Note,
                l.Details
                    .OrderBy(d => d.ItemNo)
                    .Select(d => new TrfTarifPolicyPublishLogDetailResponse(
                        d.ItemNo,
                        d.TarifId,
                        d.KelasId,
                        d.TipeTarifId,
                        d.NilaiTarifId,
                        d.Nilai))
                    .ToList()))
            .ToList();

        return Task.FromResult<IReadOnlyList<TrfTarifPolicyPublishLogResponse>>(logs);
    }
}
