using Ardalis.GuardClauses;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using MediatR;

namespace Bilreg.Application.IgdContext.TindakanIgdFeature.UseCases;

public record IgdTindakanListQuery(string IgdVisitId) : IRequest<IEnumerable<IgdTindakanListResponse>>, IIgdVisitKey;

public record IgdTindakanListResponse(
    string TindakanIgdId, string IgdVisitId, string RegId,
    ActivityTindakanIgd Aktifitas, string AktifitasString,
    string ReffId, string Descriptions, int Qty, PpaReff ppa);
public class IgdTindakanListHandler : IRequestHandler<IgdTindakanListQuery, IEnumerable<IgdTindakanListResponse>>
{
    private readonly ITindakanIgdRepo _tindakanIgdRepo;
    private readonly IIgdVisitRepo _igdVisitRepo;
    public IgdTindakanListHandler(ITindakanIgdRepo tindakanIgdRepo, 
        IIgdVisitRepo igdVisitRepo)
    {
        _tindakanIgdRepo = tindakanIgdRepo;
        _igdVisitRepo = igdVisitRepo;
    }

    public Task<IEnumerable<IgdTindakanListResponse>> Handle(IgdTindakanListQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));

        var visit = _igdVisitRepo.LoadEntity(request).GetValueOrThrow($"IgdVisit {request.IgdVisitId} not found");
        var listTindakan = _tindakanIgdRepo.ListData(request)?.ToList() ?? [];
        var result = listTindakan.Select(x => new IgdTindakanListResponse(
            x.TindakanIgdId, x.IgdVisitId, x.RegId, x.Aktifitas, x.Aktifitas.ToString(),
            x.ReffId, x.Descriptions, x.Qty, x.Ppa));

        return Task.FromResult(result);
    }
}
