using Bilreg.Application.AdmisiRanapContext.OperationalWorklistFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.OperationalWorklistFeature.UseCases;

public record AdmListOperationalWorklistQry(
    string? Jenis = null,
    string? DokterId = null,
    string? BangsalId = null,
    string? KelasId = null,
    string? TipeJaminanId = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? SearchTerm = null,
    bool IncludeTerminal = false) : IRequest<AdmListOperationalWorklistResponse>;

public record AdmListOperationalWorklistResponse(IReadOnlyList<OperationalWorklistItemView> Items);

public class AdmListOperationalWorklistHandler
    : IRequestHandler<AdmListOperationalWorklistQry, AdmListOperationalWorklistResponse>
{
    private readonly IOperationalWorklistDal _worklistDal;

    public AdmListOperationalWorklistHandler(IOperationalWorklistDal worklistDal) =>
        _worklistDal = worklistDal;

    public Task<AdmListOperationalWorklistResponse> Handle(
        AdmListOperationalWorklistQry request,
        CancellationToken cancellationToken)
    {
        var items = _worklistDal
            .List(new OperationalWorklistFilter(
                request.Jenis,
                request.DokterId,
                request.BangsalId,
                request.KelasId,
                request.TipeJaminanId,
                request.DateFrom,
                request.DateTo,
                request.SearchTerm,
                request.IncludeTerminal))
            .ToList();

        return Task.FromResult(new AdmListOperationalWorklistResponse(items));
    }
}
