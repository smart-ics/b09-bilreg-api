using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanFeature;

public record PolisRemoveCoverageCommand(string PolisId, string PasienId) : IRequest<PolisRemoveCoverageResponse>, IPolisKey, IPasienKey;

public record PolisRemoveCoverageResponse(string PolisId);

public class PolisRemoveCoverageHandler : IRequestHandler<PolisRemoveCoverageCommand, PolisRemoveCoverageResponse>
{
    private readonly IPolisRepo _polisRepo;
    private readonly IPasienRepo _pasienRepo;
    public PolisRemoveCoverageHandler(IPolisRepo polisRepo, 
        IPasienRepo pasienRepo)
    {
        _polisRepo = polisRepo;
        _pasienRepo = pasienRepo;
    }

    public Task<PolisRemoveCoverageResponse> Handle(PolisRemoveCoverageCommand request, CancellationToken cancellationToken)
    {
        var polis = _polisRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Polis {request.PolisId} not found")
            );

        var pasien = _pasienRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"pasien {request.PasienId} not found")
            );

        polis.RemoveCoverage(pasien);

        _polisRepo.SaveChanges(polis);

        return Task.FromResult(new PolisRemoveCoverageResponse(polis.PolisId));
    }
}
