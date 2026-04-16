using MediatR;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record RuangListCmd() : IRequest<IEnumerable<RuangListResponse>>;

public record RuangListResponse(string RuangId, string RuangName, string PrefixAntrian);

public class RuangListHandler : IRequestHandler<RuangListCmd, IEnumerable<RuangListResponse>>
{
    private readonly IRuangRepo _ruangRepo;

    public RuangListHandler(IRuangRepo ruangRepo)
    {
        _ruangRepo = ruangRepo;
    }

    public Task<IEnumerable<RuangListResponse>> Handle(RuangListCmd request, CancellationToken cancellationToken)
    {
        var listRuang = _ruangRepo.ListData()?.ToList() ?? [];
        var result = listRuang.Select(x => new RuangListResponse(x.RuangId, x.RuangName, x.PrefixAntrian));
        return Task.FromResult(result);
    }
}
