using Ardalis.GuardClauses;
using Farinv.Domain.SalesContext.AntrianFeature;
using MediatR;

namespace Farinv.Application.SalesContext.AntrianFeature.UsesCases;

public record QueAddAntrianCmd(int ServicePoint, int NoAntrian) : IRequest;

public class AddAntrianHandler : IRequestHandler<QueAddAntrianCmd>
{
    private readonly IAntrianRepo _antrianRepo;

    public AddAntrianHandler(IAntrianRepo antrianRepo)
    {
        _antrianRepo = antrianRepo;
    }

    public Task Handle(QueAddAntrianCmd request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NegativeOrZero(request.ServicePoint);
        Guard.Against.NegativeOrZero(request.NoAntrian);

        // BUILD
        var antrian = LoadAntrian(request);
        antrian.AddEntry(request.NoAntrian);

        //  WRITE
        _antrianRepo.SaveChanges(antrian);
        return Task.CompletedTask;
    }

    #region PRIVATE-HELPERS
    private AntrianModel LoadAntrian(QueAddAntrianCmd request)
    {
        return _antrianRepo.LoadOrCreate(request.ServicePoint);
    }
    #endregion
}