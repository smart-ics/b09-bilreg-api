using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienAddContactCommand(string PasienId, int JenisContact, string DetilContact) : IRequest, IPasienKey
{
    public string UserId { get; init; } = string.Empty;
}

public class PasienAddContactHandler : IRequestHandler<PasienAddContactCommand>
{
    private readonly IPasienRepo _pasienRepo;
    private readonly IAuditRepo _auditRepo;

    public PasienAddContactHandler(IPasienRepo pasienRepo, IAuditRepo auditRepo)
    {
        _pasienRepo = pasienRepo;
        _auditRepo = auditRepo;
    }

    public Task Handle(PasienAddContactCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(request.PasienId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pasien id {request.PasienId} not found")
            );

        var originalDataJson = AuditLogSnapshotJson.Serialize(pasien);

        var contact = new ContactType((JenisContactEnum)request.JenisContact, request.DetilContact);
        pasien.AddContact(contact);

        _pasienRepo.SaveChanges(pasien);
        var auditLog = AuditLog.Create(request.UserId, DateTime.Now, "Pasien.AddContact", "Pasien", pasien.PasienId, null, originalDataJson);
        _auditRepo.SaveChanges(auditLog);

        return Task.CompletedTask;
    }
}
