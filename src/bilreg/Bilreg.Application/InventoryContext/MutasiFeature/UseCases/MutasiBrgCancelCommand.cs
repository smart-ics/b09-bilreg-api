using Ardalis.GuardClauses;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record MutasiBrgCancelCommand(string MutasiBrgId, string UserId, string Reason = "") : IRequest, IMutasiBrgKey;

public class MutasiBrgCancelHandler : IRequestHandler<MutasiBrgCancelCommand>
{
    private readonly IMutasiBrgRepo _mutasiBrgRepo;
    private readonly IMutasiBrgMovementService _movementService;
    private readonly ISqlDatabase _sqlDatabase;
    private readonly IOrderMutasiRepo? _orderMutasiRepo;

    public MutasiBrgCancelHandler(
        IMutasiBrgRepo mutasiBrgRepo,
        IMutasiBrgMovementService movementService,
        ISqlDatabase sqlDatabase,
        IOrderMutasiRepo? orderMutasiRepo = null)
    {
        _mutasiBrgRepo = mutasiBrgRepo;
        _movementService = movementService;
        _sqlDatabase = sqlDatabase;
        _orderMutasiRepo = orderMutasiRepo;
    }

    public Task Handle(MutasiBrgCancelCommand request, CancellationToken cancellationToken)
    {
        // 1. Guard input
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.MutasiBrgId, nameof(request.MutasiBrgId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        // 2. Load MutasiBrgModel
        var maybe = _mutasiBrgRepo.LoadEntity(request);
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"MutasiBrg with Id '{request.MutasiBrgId}' not found.");

        var mutasi = maybe.Value;

        // 3. Validate state
        if (mutasi.State != MutasiBrgStateEnum.Dispatched)
            throw new InvalidOperationException($"Pembatalan hanya diizinkan jika status Dispatched. Status saat ini: {mutasi.State}");

        // 4. Atomic transaction
        _sqlDatabase.ExecuteTransaction((conn, trans) =>
        {
            mutasi.Cancel(request.UserId);
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
        });

        return Task.CompletedTask;
    }
}
