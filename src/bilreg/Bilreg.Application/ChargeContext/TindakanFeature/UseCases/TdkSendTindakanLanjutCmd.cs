using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;

//  M03-F01 P2-S04 — transitions a Proposed order to Sent.
//  State rule delegated to the aggregate; optimistic RowVersion write
//  keeps the transition from silently overwriting a concurrent change.
public record TdkSendTindakanLanjutCmd(string TindakanLanjutId, string UserId)
    : IRequest, ITindakanLanjutKey;

public class TdkSendTindakanLanjutHandler : IRequestHandler<TdkSendTindakanLanjutCmd>
{
    private readonly ITindakanLanjutRepo _tindakanLanjutRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public TdkSendTindakanLanjutHandler(ITindakanLanjutRepo tindakanLanjutRepo,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider)
    {
        _tindakanLanjutRepo = tindakanLanjutRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(TdkSendTindakanLanjutCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TindakanLanjutId, nameof(request.TindakanLanjutId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var order = _tindakanLanjutRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"Tindakan lanjut {request.TindakanLanjutId} not found")
            );

        var snapshotJson = AuditLogSnapshotJson.Serialize(order);
        var occurredAt = _tglJamProvider.Now;
        order.Send(request.UserId, occurredAt);

        using var trans = TransHelper.NewScope();
        _tindakanLanjutRepo.SaveChanges(order);
        _auditRepo.SaveChanges(CreateAudit(order, snapshotJson, request, occurredAt));
        trans.Complete();

        return Task.CompletedTask;
    }

    private static AuditLog CreateAudit(TindakanLanjutModel order, string snapshotJson,
        TdkSendTindakanLanjutCmd cmd, DateTime occurredAt)
    {
        return AuditLog.Create(
            order.AuditTrail.Modified,
            actionType: "SEND",
            entityName: nameof(TindakanLanjutModel),
            entityId: order.TindakanLanjutId,
            originalDataJson: snapshotJson,
            correlationId: order.Reg.RegId);
    }
}
