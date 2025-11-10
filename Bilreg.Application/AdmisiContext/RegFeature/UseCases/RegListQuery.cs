using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using CommunityToolkit.Diagnostics;
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
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.TglYmd);
        Guard.IsNotWhiteSpace(request.LayananId);

        var tgl = request.TglYmd.ToDate("yyyy-MM-dd");
        var periode = new Periode(tgl);

        var listReg = _regRepo.ListData(periode, request);

        return Task.FromResult(listReg);
    }
}
