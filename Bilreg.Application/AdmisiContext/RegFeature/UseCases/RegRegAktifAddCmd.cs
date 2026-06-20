using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegRegAktifAddCmd(string RegId) : IRequest, IRegKey;

public class RegRegAktifAddHandler : IRequestHandler<RegRegAktifAddCmd>
{
    private readonly IRegRepo _regRepo;
    private readonly IRegAktifRepo _regAktifRepo;
    public RegRegAktifAddHandler(IRegRepo regRepo, 
        IRegAktifRepo regAktifRepo)
    {
        _regRepo = regRepo;
        _regAktifRepo = regAktifRepo;
    }

    public Task Handle(RegRegAktifAddCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));

        var reg = _regRepo.LoadEntity(request).GetValueOrThrow($"Register {request.RegId} not found");

        var regAktif = RegAktifModel.CreateFromReg(reg);

        _regAktifRepo.SaveChanges(regAktif);
        return Task.CompletedTask;
    }
}
