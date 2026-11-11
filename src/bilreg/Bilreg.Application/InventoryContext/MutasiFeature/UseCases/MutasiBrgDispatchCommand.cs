using Ardalis.GuardClauses;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record MutasiBrgDispatchCommand(string MutasiBrgId, string UserId) : IRequest, IMutasiBrgKey;

public class MutasiBrgDispatchHandler : IRequestHandler<MutasiBrgDispatchCommand>
{
    private readonly IMutasiBrgRepo _mutasiBrgRepo;
    private readonly IMutasiBrgMovementService _movementService;
    private readonly ISqlDatabase _sqlDatabase;
    private readonly IOrderMutasiRepo? _orderMutasiRepo;

    public MutasiBrgDispatchHandler(
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

    public Task Handle(MutasiBrgDispatchCommand request, CancellationToken cancellationToken)
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

        // 3. Validate state & items
        if (mutasi.State != MutasiBrgStateEnum.Draft)
            throw new InvalidOperationException($"Hanya dokumen berstatus Draft yang dapat di-dispatch. Status saat ini: {mutasi.State}.");

        if (mutasi.ListItem.Count == 0)
            throw new InvalidOperationException("Mutasi barang harus memiliki minimal 1 item untuk di-dispatch.");

        // 4. Atomic transaction
        _sqlDatabase.ExecuteTransaction((conn, trans) =>
        {
            mutasi.Dispatch(request.UserId);
            _movementService.DeductStock(mutasi, conn, trans);

            if (!string.IsNullOrWhiteSpace(mutasi.OrderMutasiId) && _orderMutasiRepo is not null)
            {
                var orderKey = OrderMutasiModel.Key(mutasi.OrderMutasiId);
                var orderMaybe = _orderMutasiRepo.LoadEntity(orderKey);
                if (orderMaybe.HasValue)
                {
                    var orderMutasi = orderMaybe.Value;
                    if (orderMutasi.State == OrderMutasiStateEnum.Approved)
                    {
                        var isFulfilled = orderMutasi.ListItem.Count > 0 && orderMutasi.ListItem.All(orderItem =>
                        {
                            var dispatchedQty = mutasi.ListItem
                                .Where(x => x.Brg.BrgId == orderItem.Brg.BrgId)
                                .Sum(x => x.QtyKirim);
                            return dispatchedQty >= orderItem.Qty;
                        });

                        if (isFulfilled)
                        {
                            orderMutasi.Complete();
                            _orderMutasiRepo.SaveChanges(orderMutasi);
                        }
                    }
                }
            }

            _mutasiBrgRepo.SaveChanges(mutasi);
        });

        return Task.CompletedTask;
    }
}
