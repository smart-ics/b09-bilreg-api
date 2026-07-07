using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature.UseCases;

public record AdmCancelOpnameRequestCmd(
    string OpnameRequestId,
    string UserId) : IRequest, IOpnameRequestKey;

public class AdmCancelOpnameRequestHandler : IRequestHandler<AdmCancelOpnameRequestCmd>
{
    private readonly IOpnameRequestRepo _opnameRequestRepo;

    public AdmCancelOpnameRequestHandler(IOpnameRequestRepo opnameRequestRepo) =>
        _opnameRequestRepo = opnameRequestRepo;

    public Task Handle(AdmCancelOpnameRequestCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OpnameRequestId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var opnameRequest = _opnameRequestRepo.LoadEntity(request)
            .GetValueOrThrow($"Opname Request '{request.OpnameRequestId}' tidak ditemukan.");
        var cancelled = opnameRequest.Cancel(request.UserId);

        _opnameRequestRepo.SaveChanges(cancelled);
        return Task.CompletedTask;
    }
}
