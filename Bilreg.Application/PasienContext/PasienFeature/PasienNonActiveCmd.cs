using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienNonActiveCmd(string PasienId) : IRequest, IPasienKey;

public class PasienNonActiveHandler : IRequestHandler<PasienNonActiveCmd>
{
    private readonly IPasienRepo _pxRepo;

    public PasienNonActiveHandler(IPasienRepo pxRepo)
    {
        _pxRepo = pxRepo;
    }

    public Task Handle(PasienNonActiveCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienId);

        var pasien = _pxRepo.LoadEntity(request).GetValueOrThrow($"Pasien {request.PasienId} not found");
        pasien.NonActive();

        _pxRepo.SaveChanges(pasien);

        return Task.CompletedTask;
    }
}
