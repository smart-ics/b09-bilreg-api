using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases
{
    public record PakaiBedVoidCommand(
        string PakaiBedId,
        string UserId,
        string VoidReason,
        string UserAgent,
        string RemoteIpAddress)
        : IRequest, IPakaiBed;

    public class PakaiBedVoidCommandHandler : IRequestHandler<PakaiBedVoidCommand>
    {
        private readonly IPakaiBedAlokasiRepo _pakaiBedAlokasiRepo;
        private readonly IPakaiBedBillingLockChecker _billingLockChecker;
        private readonly IAuditRepo _auditRepo;
        private readonly ITglJamProvider _tglJamProvider;

        public PakaiBedVoidCommandHandler(
            IPakaiBedAlokasiRepo pakaiBedAlokasiRepo,
            IPakaiBedBillingLockChecker billingLockChecker,
            IAuditRepo auditRepo,
            ITglJamProvider tglJamProvider)
        {
            _pakaiBedAlokasiRepo = pakaiBedAlokasiRepo;
            _billingLockChecker = billingLockChecker;
            _auditRepo = auditRepo;
            _tglJamProvider = tglJamProvider;
        }

        public async Task Handle(PakaiBedVoidCommand request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.PakaiBedId);
            Guard.Against.NullOrWhiteSpace(request.UserId);
            Guard.Against.NullOrWhiteSpace(request.VoidReason);

            var alokasi = _pakaiBedAlokasiRepo.LoadEntity(PakaiBedAlokasiModel.Key(request.PakaiBedId))
                .GetValueOrThrow($"Trs. Pakai Bed {request.PakaiBedId} not found");

            var lockStatus = await _billingLockChecker.CheckLockStatusAsync(alokasi.PakaiBedId, alokasi.RegId, cancellationToken);
            if (lockStatus is null || lockStatus.IsLocked)
            {
                var reason = lockStatus?.Reason ?? "Billing lock status could not be verified (Fail-Closed)";
                throw new InvalidOperationException(
                    $"Pembatalan penempatan tempat tidur {alokasi.PakaiBedId} ditolak karena tagihan terkunci: {reason}");
            }

            var snapshotJson = AuditLogSnapshotJson.Serialize(alokasi);

            var occurredAt = _tglJamProvider.Now;
            var voidAt = occurredAt.Kind == DateTimeKind.Utc
                ? occurredAt
                : DateTime.SpecifyKind(occurredAt, DateTimeKind.Utc);

            var voidAlokasi = alokasi.Void(voidAt, request.VoidReason, request.UserId, voidAt);

            _pakaiBedAlokasiRepo.SaveChanges(voidAlokasi);

            var audit = CreateAudit(voidAlokasi, snapshotJson, request, voidAt);
            _auditRepo.SaveChanges(audit);
        }

        private static AuditLog CreateAudit(
            PakaiBedAlokasiModel alokasi,
            string snapshotJson,
            PakaiBedVoidCommand cmd,
            DateTime voidAt)
        {
            var result = AuditLog.Create(
                new AuditInfoType(cmd.UserId, voidAt),
                actionType: "VOID",
                entityName: nameof(PakaiBedModel),
                entityId: alokasi.PakaiBedId,
                reason: cmd.VoidReason,
                originalDataJson: snapshotJson,
                correlationId: alokasi.RegId,
                clientIpAddress: cmd.RemoteIpAddress,
                userAgent: cmd.UserAgent);
            return result;
        }
    }
}