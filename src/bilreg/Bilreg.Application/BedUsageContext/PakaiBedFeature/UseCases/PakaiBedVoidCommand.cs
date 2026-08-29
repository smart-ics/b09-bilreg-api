using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases
{
    public record PakaiBedVoidCommand(
        string PakaiBedId, string UserId, string VoidReason, string UserAgent, string RemoteIpAddress)
        : IRequest, IPakaiBed;

    public class PakaiBedDeleteCommandHandler : IRequestHandler<PakaiBedVoidCommand>
    {
        private readonly IPakaiBedRepo _pakaiBedRepo;
        private readonly IAuditRepo _auditRepo;
        private readonly ITglJamProvider _tglJamProvider;

        public PakaiBedDeleteCommandHandler(IPakaiBedRepo pakaiBedRepo, 
            IAuditRepo auditRepo, 
            ITglJamProvider tglJamProvider)
        {
            _pakaiBedRepo = pakaiBedRepo;
            _auditRepo = auditRepo;
            _tglJamProvider = tglJamProvider;
        }

        public Task Handle(PakaiBedVoidCommand request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.PakaiBedId);
            Guard.Against.NullOrWhiteSpace(request.UserId);
            Guard.Against.NullOrWhiteSpace(request.VoidReason);

            var saved = _pakaiBedRepo.LoadEntity(PakaiBedModel.Key(request.PakaiBedId))
                .GetValueOrThrow($"Trs. Pakai Bed {request.PakaiBedId} not found");

            var snapshotJson = AuditLogSnapshotJson.Serialize(saved);

            var occurredAt = _tglJamProvider.Now;

            saved.Void(request.UserId, occurredAt);

            _pakaiBedRepo.SaveChanges(saved);

            var audit = CreateAudit(saved, snapshotJson, request);
            _auditRepo.SaveChanges(audit);

            return Task.CompletedTask;
        }

        private AuditLog CreateAudit(PakaiBedModel pkb, string snapShotJson, PakaiBedVoidCommand cmd)
        {
            var result = AuditLog.Create(
                pkb.AuditTrail.Voided,
                actionType: "VOID",
                entityName: nameof(TindakanModel),
                entityId: pkb.PakaiBedId,
                reason: cmd.VoidReason,
                originalDataJson: snapShotJson,
                correlationId: pkb.Reg.RegId,
                clientIpAddress: cmd.RemoteIpAddress,
                userAgent: cmd.UserAgent
                );
            return result;
        }
    }
}