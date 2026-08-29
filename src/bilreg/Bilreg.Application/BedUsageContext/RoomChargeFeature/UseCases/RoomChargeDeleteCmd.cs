using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.BedUsageContext.RoomChargeFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.RoomChargeFeature.UseCases;

public record RoomChargeDeleteCmd(string RoomChargeId, string UserId, string VoidReason,
    string ClientIpAddress, string UserAgent) : IRequest, IRoomChargeKey;

public class RoomChargeDeleteHandler : IRequestHandler<RoomChargeDeleteCmd>
{
    private readonly IRoomChargeRepo _roomChargeRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;
    public RoomChargeDeleteHandler(IRoomChargeRepo roomChargeRepo, 
        IAuditRepo auditRepo, 
        ITglJamProvider tglJamProvider)
    {
        _roomChargeRepo = roomChargeRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(RoomChargeDeleteCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RoomChargeId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        Guard.Against.NullOrWhiteSpace(request.VoidReason);
        cancellationToken.ThrowIfCancellationRequested();

        var roomCharge = _roomChargeRepo.LoadEntity(request)
            .GetValueOrThrow($"Room Charge {request.RoomChargeId} not found");
        var snapshotJson = AuditLogSnapshotJson.Serialize(roomCharge);
        cancellationToken.ThrowIfCancellationRequested();

        using (var trans = TransHelper.NewScope())
        {
            var occurredAt = _tglJamProvider.Now;
            _roomChargeRepo.Delete(request, roomCharge, occurredAt, request.UserId);

            var audit = CreateAudit(roomCharge, snapshotJson, request, occurredAt);
            _auditRepo.SaveChanges(audit);
            trans.Complete();
        }
        return Task.CompletedTask;
    }

    private AuditLog CreateAudit(RoomChargeModel roomCharge, string snapShotJson, RoomChargeDeleteCmd cmd, DateTime occurredAt)
    {
        var auditInfo = new AuditInfoType(cmd.UserId, occurredAt);
        var result = AuditLog.Create(
            auditInfo,
            actionType: "DELETE",
            entityName: nameof(RoomChargeModel),
            entityId: roomCharge.RoomChargeId,
            reason: cmd.VoidReason,
            originalDataJson: snapShotJson,
            correlationId: roomCharge.Reg.RegId,
            clientIpAddress: cmd.ClientIpAddress,
            userAgent: cmd.UserAgent
            );
        return result;
    }
}
