using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;

public record MutasiBerkasHistoriQuery(string MrId) : IRequest<IEnumerable<MutasiBerkasHistoriItemDto>>;

public class MutasiBerkasHistoriHandler : IRequestHandler<MutasiBerkasHistoriQuery, IEnumerable<MutasiBerkasHistoriItemDto>>
{
    private readonly IMutasiBerkasWorklistDal _worklistDal;

    public MutasiBerkasHistoriHandler(IMutasiBerkasWorklistDal worklistDal)
    {
        _worklistDal = worklistDal;
    }

    public Task<IEnumerable<MutasiBerkasHistoriItemDto>> Handle(MutasiBerkasHistoriQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.MrId, nameof(request.MrId));

        var result = _worklistDal.QueryHistori(request.MrId) ?? [];
        return Task.FromResult(result);
    }
}
