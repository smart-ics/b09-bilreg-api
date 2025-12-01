using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegListQuery(string TglYmd, string LayananId) : IRequest<IEnumerable<RegView>>, ILayananKey;

public class RegListHandler : IRequestHandler<RegListQuery, IEnumerable<RegView>>
{
    private readonly IRegRepo _regRepo;

    public RegListHandler(IRegRepo regRepo)
    {
        _regRepo = regRepo;
    }

    public Task<IEnumerable<RegView>> Handle(RegListQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);
        Guard.Against.NullOrWhiteSpace(request.TglYmd);
        Guard.Against.NullOrWhiteSpace(request.LayananId);

        var tgl = request.TglYmd.ToDate("yyyy-MM-dd");
        var periode = new Periode(tgl);

        var listReg = _regRepo.ListData(periode, request);

        return Task.FromResult(listReg);
    }
}
