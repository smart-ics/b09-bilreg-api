using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienReActiveCmd(string PasienId) : IRequest, IPasienKey;

public class PasienReActiveHandler : IRequestHandler<PasienReActiveCmd>
{
    private readonly IPasienRepo _pxRepo;

    public PasienReActiveHandler(IPasienRepo pxRepo)
    {
        _pxRepo = pxRepo;
    }

    public Task Handle(PasienReActiveCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienId);

        var pasien = _pxRepo.LoadEntity(request).GetValueOrThrow($"Pasien {request.PasienId} not found");
        pasien.ReActive();

        _pxRepo.SaveChanges(pasien);

        return Task.CompletedTask;
    }
}
