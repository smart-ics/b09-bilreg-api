using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Farinv.Domain.SalesContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using MediatR;

namespace Farinv.Application.SalesContext.AntrianFeature.UsesCases;

public record QueAddAntrianByRegCmd(string RegId, int ServicePoint, int NoAntrian)
    : IRequest, IRegKey;

public class AddAntrianByRegHandler : IRequestHandler<QueAddAntrianByRegCmd>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IRegRepo _regRepo;

    public AddAntrianByRegHandler(IAntrianRepo antrianRepo, IRegRepo regRepo)
    {
        _antrianRepo = antrianRepo;
        _regRepo = regRepo;
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
        var reg = _regRepo.LoadEntity(request).GetValueOrThrow($"Register {request.RegId} not found");
        return reg.ToReff();
    }
    #endregion
}