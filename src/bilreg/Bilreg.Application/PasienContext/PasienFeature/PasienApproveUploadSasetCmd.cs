using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienApproveUploadSasetCmd(string PasienId) : IRequest, IPasienKey;

public class PasienApproveUploadSasetHandler : IRequestHandler<PasienApproveUploadSasetCmd>
{
    private readonly IPasienRepo _pasienRepo;

    public PasienApproveUploadSasetHandler(IPasienRepo pasienRepo)
    {
        _pasienRepo = pasienRepo;
    }

    public Task Handle(PasienApproveUploadSasetCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienId);

        var pasien = _pasienRepo.LoadEntity(request).GetValueOrThrow($"Pasien {request.PasienId} not found");
        pasien.ApproveUploadSaset(DateTime.Now);

        _pasienRepo.SaveChanges(pasien);

        return Task.CompletedTask;
    }
}
