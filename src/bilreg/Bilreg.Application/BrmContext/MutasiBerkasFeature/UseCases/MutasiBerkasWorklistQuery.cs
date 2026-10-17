using MediatR;

namespace Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;

public record MutasiBerkasWorklistQuery(
    string? TglPraktek = null,
    string? LayananId = null,
    string? SesiPoli = null,
    int? Status = null,
    string? GedungLantai = null
) : IRequest<IEnumerable<MutasiBerkasWorklistItemDto>>;

public class MutasiBerkasWorklistHandler : IRequestHandler<MutasiBerkasWorklistQuery, IEnumerable<MutasiBerkasWorklistItemDto>>
{
    private readonly IMutasiBerkasWorklistDal _worklistDal;

    public MutasiBerkasWorklistHandler(IMutasiBerkasWorklistDal worklistDal)
    {
        _worklistDal = worklistDal;
    }

    public Task<IEnumerable<MutasiBerkasWorklistItemDto>> Handle(MutasiBerkasWorklistQuery request, CancellationToken cancellationToken)
    {
        var filter = new MutasiBerkasWorklistFilter(
            request.TglPraktek,
            request.LayananId,
            request.SesiPoli,
            request.Status,
            request.GedungLantai
        );

        var result = _worklistDal.QueryWorklist(filter) ?? [];
        return Task.FromResult(result);
    }
}
