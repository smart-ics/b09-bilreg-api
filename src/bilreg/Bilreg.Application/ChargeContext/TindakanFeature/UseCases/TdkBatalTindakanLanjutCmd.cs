using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;

//  M03-F01 P2-S04 — cancels an order only while Proposed or Sent.
//  A received order is rejected by the aggregate (Cancel rejects
//  cancellation of a received order); the optimistic RowVersion write
//  lets a concurrent cancellation lose against a confirmed reception.
public record TdkBatalTindakanLanjutCmd(
    string TindakanLanjutId, string UserId, string CancelReason)
    : IRequest, ITindakanLanjutKey;

public class TdkBatalTindakanLanjutHandler : IRequestHandler<TdkBatalTindakanLanjutCmd>
{
    private readonly ITindakanLanjutRepo _tindakanLanjutRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public TdkBatalTindakanLanjutHandler(ITindakanLanjutRepo tindakanLanjutRepo,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider)
    {
        _tindakanLanjutRepo = tindakanLanjutRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(TdkBatalTindakanLanjutCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TindakanLanjutId, nameof(request.TindakanLanjutId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.CancelReason, nameof(request.CancelReason));

        var order = _tindakanLanjutRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"Tindakan lanjut {request.TindakanLanjutId} not found")
            );

        var snapshotJson = AuditLogSnapshotJson.Serialize(order);
        var occurredAt = _tglJamProvider.Now;
        order.Cancel(request.CancelReason, request.UserId, occurredAt);

        using var trans = TransHelper.NewScope();
        _tindakanLanjutRepo.SaveChanges(order);
        _auditRepo.SaveChanges(CreateAudit(order, snapshotJson, request));
        trans.Complete();

        return Task.CompletedTask;
    }

    private static AuditLog CreateAudit(TindakanLanjutModel order, string snapshotJson,
        TdkBatalTindakanLanjutCmd cmd)
    {
        return AuditLog.Create(
            order.AuditTrail.Voided,
            actionType: "VOID",
            entityName: nameof(TindakanLanjutModel),
            entityId: order.TindakanLanjutId,
            reason: cmd.CancelReason,
            originalDataJson: snapshotJson,
            correlationId: order.Reg.RegId);
    }
}
