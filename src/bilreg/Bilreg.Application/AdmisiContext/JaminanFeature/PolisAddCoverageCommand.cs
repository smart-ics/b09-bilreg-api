using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanFeature;

public record PolisAddCoverageCommand(string PolisId, string PasienId, string StatusPesertaId) : 
    IRequest<PolisAddCoverageResponse>, IPolisKey, IPasienKey;

public record PolisAddCoverageResponse(string PolisId);

public class PolisAddCoverageHandler : IRequestHandler<PolisAddCoverageCommand, PolisAddCoverageResponse>
{
    private readonly IPolisRepo _polisRepo;
    private readonly IPasienRepo _pasienRepo;
    public PolisAddCoverageHandler(IPolisRepo polisRepo, 
        IPasienRepo pasienRepo)
    {
        _polisRepo = polisRepo;
        _pasienRepo = pasienRepo;
    }

    public Task<PolisAddCoverageResponse> Handle(PolisAddCoverageCommand request, CancellationToken cancellationToken)
    {
        var polis = _polisRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () =>  throw new KeyNotFoundException($"Polis {request.PolisId} not found")
            );

        var pasien = _pasienRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () =>  throw new KeyNotFoundException($"pasien {request.PasienId} not found")
            );

        var statusPeserta = StatusPesertaType.Create(request.StatusPesertaId);
        
        polis.AddCoverage(pasien, statusPeserta);

        _polisRepo.SaveChanges(polis);

        return Task.FromResult( new PolisAddCoverageResponse(polis.PolisId));
    }
}
