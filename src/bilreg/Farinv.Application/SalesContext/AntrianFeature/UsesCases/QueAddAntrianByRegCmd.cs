using Ardalis.GuardClauses;
using Farinv.Domain.SalesContext.AntrianFeature;
using MediatR;

namespace Farinv.Application.SalesContext.AntrianFeature.UsesCases;

public record QueAddAntrianByRegCmd(string RegId, int ServicePoint, int NoAntrian)
    : IRequest, IRegKey;

public class AddAntrianByRegHandler : IRequestHandler<QueAddAntrianByRegCmd>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IGetRegService _getRegService;

    public AddAntrianByRegHandler(IAntrianRepo antrianRepo, IGetRegService getRegService)
    {
        _antrianRepo = antrianRepo;
        _getRegService = getRegService;
    }

    public Task Handle(QueAddAntrianByRegCmd request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NegativeOrZero(request.NoAntrian);
        Guard.Against.NegativeOrZero(request.ServicePoint);

        // BUILD
        var reg = LoadReg(request);
        var antrian = LoadAntrian(request);
        antrian.AddEntry(request.NoAntrian, reg);

        //  WRITE
        _antrianRepo.SaveChanges(antrian);
        return Task.CompletedTask;
    }

    #region PRIVATE-HELPERS
    private AntrianModel LoadAntrian(QueAddAntrianByRegCmd request)
    {
        return _antrianRepo.LoadOrCreate(request.ServicePoint);
    }

    private RegReff LoadReg(QueAddAntrianByRegCmd request)
    {
        var reg = _getRegService.Execute(request) 
            ?? throw new KeyNotFoundException($"Reg {request.RegId} not found");
        return reg.ToReff();
    }
    #endregion
}