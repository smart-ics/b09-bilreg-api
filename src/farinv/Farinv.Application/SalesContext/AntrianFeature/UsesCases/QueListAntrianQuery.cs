using Ardalis.GuardClauses;
using Farinv.Domain.SalesContext.AntrianFeature;
using Farinv.Domain.Shared.Helpers;
using MediatR;

namespace Farinv.Application.SalesContext.AntrianFeature.UsesCases;

public record QueListAntrianQuery(string TglYmd) : IRequest<IEnumerable<AntrianListResponse>>;

public record AntrianListResponse(string AntrianId, int ServicePoint, 
    int NoAntrian, int StatusAntrian, string StatusAntrianString,
    RegReff Reg, string ReffId, string ReffDesc);

public class AntrianListHandler
    : IRequestHandler<QueListAntrianQuery, IEnumerable<AntrianListResponse>>
{
    private readonly IAntrianRepo _antrianRepo;

    public AntrianListHandler(IAntrianRepo antrianRepo)
    {
        _antrianRepo = antrianRepo;
    }

    public Task<IEnumerable<AntrianListResponse>> Handle(QueListAntrianQuery request, 
        CancellationToken cancellationToken)
    {
        Guard.Against.InvalidDateFormat(request.TglYmd, nameof(request.TglYmd));
        
        var result = LoadAntrian(request);
        return Task.FromResult(result);
    }

    private IEnumerable<AntrianListResponse> LoadAntrian(QueListAntrianQuery request)
    {
        var date = DateTime.Parse(request.TglYmd);
        var listAntrian = _antrianRepo.ListData(date);

        var result = listAntrian.Select(x => new AntrianListResponse(x.AntrianId, x.ServicePoint, 
            x.NoAntrian, (int)x.AntrianStatus, x.AntrianStatus.ToString(),
            new RegReff(x.RegId, x.PasienId, x.PasienName), x.ReffId, x.ReffDesc
            ));
        return result;
    }
}