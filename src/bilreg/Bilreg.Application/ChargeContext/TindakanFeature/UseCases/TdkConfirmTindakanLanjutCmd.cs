using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;

//  M03-F01 P2-S04 — records reception confirmation (Sent -> Received, TD-07).
//  Idempotent: a repeated confirmation returns IsNewConfirmation = false
//  and writes neither a second transition nor a second audit entry.
public record TdkConfirmTindakanLanjutCmd(
    string TindakanLanjutId, string ReceivedBy, string UserId)
    : IRequest<TdkConfirmTindakanLanjutResponse>, ITindakanLanjutKey;

public record TdkConfirmTindakanLanjutResponse(bool IsNewConfirmation);

public class TdkConfirmTindakanLanjutHandler
    : IRequestHandler<TdkConfirmTindakanLanjutCmd, TdkConfirmTindakanLanjutResponse>
{
    private readonly ITindakanLanjutRepo _tindakanLanjutRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public TdkConfirmTindakanLanjutHandler(ITindakanLanjutRepo tindakanLanjutRepo,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider)
    {
        _tindakanLanjutRepo = tindakanLanjutRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<TdkConfirmTindakanLanjutResponse> Handle(
        TdkConfirmTindakanLanjutCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TindakanLanjutId, nameof(request.TindakanLanjutId));
        Guard.Against.NullOrWhiteSpace(request.ReceivedBy, nameof(request.ReceivedBy));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var order = _tindakanLanjutRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"Tindakan lanjut {request.TindakanLanjutId} not found")
            );

        var snapshotJson = AuditLogSnapshotJson.Serialize(order);
        var occurredAt = _tglJamProvider.Now;
        var isNew = order.ConfirmReceived(request.ReceivedBy, request.UserId, occurredAt);
        if (!isNew)
            return Task.FromResult(new TdkConfirmTindakanLanjutResponse(false));

        using var trans = TransHelper.NewScope();
        _tindakanLanjutRepo.SaveChanges(order);
        _auditRepo.SaveChanges(CreateAudit(order, snapshotJson, request, occurredAt));
        trans.Complete();

        return Task.FromResult(new TdkConfirmTindakanLanjutResponse(true));
    }

    private static AuditLog CreateAudit(TindakanLanjutModel order, string snapshotJson,
        TdkConfirmTindakanLanjutCmd cmd, DateTime occurredAt)
    {
        return AuditLog.Create(
            order.AuditTrail.Modified,
            actionType: "CONFIRM",
            entityName: nameof(TindakanLanjutModel),
            entityId: order.TindakanLanjutId,
            reason: cmd.ReceivedBy,
            originalDataJson: snapshotJson,
            correlationId: order.Reg.RegId);
    }
}
