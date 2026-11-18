using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

[method: JsonConstructor]
public record MutasiBrgCancelCommand(string MutasiBrgId, string UserId, string Reason) : IRequest, IMutasiBrgKey
{
    [Obsolete("Use constructor with Reason")]
    public MutasiBrgCancelCommand(string mutasiBrgId, string userId)
        : this(mutasiBrgId, userId, string.Empty)
    {
    }
}

public class MutasiBrgCancelHandler : IRequestHandler<MutasiBrgCancelCommand>
{
    private readonly IMutasiBrgRepo _mutasiBrgRepo;
    private readonly IMutasiBrgMovementService _movementService;
    private readonly ISqlDatabase _sqlDatabase;
    private readonly IAuditRepo _auditRepo;
    private readonly IOrderMutasiRepo? _orderMutasiRepo;

    public MutasiBrgCancelHandler(
        IMutasiBrgRepo mutasiBrgRepo,
        IMutasiBrgMovementService movementService,
        ISqlDatabase sqlDatabase,
        IAuditRepo auditRepo,
        IOrderMutasiRepo? orderMutasiRepo = null)
    {
        _mutasiBrgRepo = mutasiBrgRepo;
        _movementService = movementService;
        _sqlDatabase = sqlDatabase;
        _auditRepo = auditRepo;
        _orderMutasiRepo = orderMutasiRepo;
    }

    [Obsolete("Use constructor with IAuditRepo")]
    public MutasiBrgCancelHandler(
        IMutasiBrgRepo mutasiBrgRepo,
        IMutasiBrgMovementService movementService,
        ISqlDatabase sqlDatabase,
        IOrderMutasiRepo? orderMutasiRepo = null)
        : this(mutasiBrgRepo, movementService, sqlDatabase, null!, orderMutasiRepo)
    {
    }

    public Task Handle(MutasiBrgCancelCommand request, CancellationToken cancellationToken)
    {
        // 1. Guard input
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.MutasiBrgId, nameof(request.MutasiBrgId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason), "Alasan pembatalan tidak boleh kosong");
        if (request.Reason.Trim().Length < 5)
            throw new ArgumentException("Alasan pembatalan minimal 5 karakter", nameof(request.Reason));

        // 2. Load MutasiBrgModel
        var maybe = _mutasiBrgRepo.LoadEntity(request);
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"MutasiBrg with Id '{request.MutasiBrgId}' not found.");

        var mutasi = maybe.Value;

        // 3. Validate state
        if (mutasi.State != MutasiBrgStateEnum.Dispatched)
            throw new InvalidOperationException($"Pembatalan hanya diizinkan jika status Dispatched. Status saat ini: {mutasi.State}");

        // 4. Snapshot before state changes
        var snapshotJson = AuditLogSnapshotJson.Serialize(mutasi);

        // 5. Atomic transaction
        _sqlDatabase.ExecuteTransaction((conn, trans) =>
        {
            mutasi.Cancel(request.Reason, request.UserId);
            _movementService.RestoreStock(mutasi, conn, trans);

            if (!string.IsNullOrWhiteSpace(mutasi.OrderMutasiId) && _orderMutasiRepo is not null)
            {
                var orderKey = OrderMutasiModel.Key(mutasi.OrderMutasiId);
                var orderMaybe = _orderMutasiRepo.LoadEntity(orderKey);
                if (orderMaybe.HasValue)
                {
                    var orderMutasi = orderMaybe.Value;
                    if (orderMutasi.State == OrderMutasiStateEnum.Completed)
                    {
                        orderMutasi.Reopen();
                        _orderMutasiRepo.SaveChanges(orderMutasi);
                    }
                }
            }

            _mutasiBrgRepo.SaveChanges(mutasi);

            var auditLog = AuditLog.Create(
                request.UserId,
                DateTime.Now,
                actionType: "VOID",
                entityName: nameof(MutasiBrgModel),
                entityId: mutasi.MutasiBrgId,
                reason: request.Reason.Trim(),
                originalDataJson: snapshotJson,
                correlationId: mutasi.OrderMutasiId);
            _auditRepo?.SaveChanges(auditLog);
        });

        return Task.CompletedTask;
    }
}
