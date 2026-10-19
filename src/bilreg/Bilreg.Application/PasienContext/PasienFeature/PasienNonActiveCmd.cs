using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienNonActiveCmd(string PasienId) : IRequest, IPasienKey
{
    public string UserId { get; init; } = string.Empty;
}

public class PasienNonActiveHandler : IRequestHandler<PasienNonActiveCmd>
{
    private readonly IPasienRepo _pxRepo;
    private readonly IAuditRepo _auditRepo;

    public PasienNonActiveHandler(IPasienRepo pxRepo, IAuditRepo auditRepo)
    {
        _pxRepo = pxRepo;
        _auditRepo = auditRepo;
    }

    public Task Handle(PasienNonActiveCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienId, nameof(request.PasienId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var pasien = _pxRepo.LoadEntity(request).GetValueOrThrow($"Pasien {request.PasienId} not found");
        var originalDataJson = AuditLogSnapshotJson.Serialize(pasien);

        pasien.NonActive();

        _pxRepo.SaveChanges(pasien);
        var auditLog = AuditLog.Create(request.UserId, DateTime.Now, "Pasien.NonActive", "Pasien", pasien.PasienId, null, originalDataJson);
        _auditRepo.SaveChanges(auditLog);

        return Task.CompletedTask;
    }
}
