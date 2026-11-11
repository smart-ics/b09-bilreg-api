using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using MediatR;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record MutasiBrgCreateDraftCommand(
    string LayananAsalId,
    string LayananTujuanId,
    string OrderMutasiId = "",
    string DispatchNote = "",
    string UserId = ""
) : IRequest<MutasiBrgCreateDraftResponse>;

public record MutasiBrgCreateDraftResponse(string MutasiBrgId) : IMutasiBrgKey;

public class MutasiBrgCreateDraftHandler : IRequestHandler<MutasiBrgCreateDraftCommand, MutasiBrgCreateDraftResponse>
{
    private readonly ILayananRepo _layananRepo;
    private readonly IOrderMutasiRepo _orderMutasiRepo;
    private readonly IMutasiBrgRepo _mutasiBrgRepo;

    public MutasiBrgCreateDraftHandler(
        ILayananRepo layananRepo,
        IOrderMutasiRepo orderMutasiRepo,
        IMutasiBrgRepo mutasiBrgRepo)
    {
        _layananRepo = layananRepo;
        _orderMutasiRepo = orderMutasiRepo;
        _mutasiBrgRepo = mutasiBrgRepo;
    }

    public Task<MutasiBrgCreateDraftResponse> Handle(MutasiBrgCreateDraftCommand request, CancellationToken cancellationToken)
    {
        // 1. Guard input
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.LayananAsalId, nameof(request.LayananAsalId));
        Guard.Against.NullOrWhiteSpace(request.LayananTujuanId, nameof(request.LayananTujuanId));

        if (request.LayananAsalId.Trim().Equals(request.LayananTujuanId.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Unit asal dan tujuan tidak boleh sama");

        // 2. Validate LayananAsal and LayananTujuan existence
        var asalMaybe = _layananRepo.LoadEntity(LayananType.Key(request.LayananAsalId));
        if (!asalMaybe.HasValue)
            throw new KeyNotFoundException($"LayananAsalId '{request.LayananAsalId}' not found");
        var asal = asalMaybe.Value;

        var tujuanMaybe = _layananRepo.LoadEntity(LayananType.Key(request.LayananTujuanId));
        if (!tujuanMaybe.HasValue)
            throw new KeyNotFoundException($"LayananTujuanId '{request.LayananTujuanId}' not found");
        var tujuan = tujuanMaybe.Value;

        // 3. Validate OrderMutasi if provided
        var orderMutasiId = request.OrderMutasiId?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(orderMutasiId) && orderMutasiId != "-")
        {
            var orderMaybe = _orderMutasiRepo.LoadEntity(OrderMutasiModel.Key(orderMutasiId));
            if (!orderMaybe.HasValue)
                throw new KeyNotFoundException($"OrderMutasi '{orderMutasiId}' not found");

            var order = orderMaybe.Value;
            if (order.State != OrderMutasiStateEnum.Approved)
                throw new InvalidOperationException($"OrderMutasi '{orderMutasiId}' is not in Approved state (Current: {order.State})");

            if (!string.IsNullOrWhiteSpace(order.LayananTujuan.LayananId) && order.LayananTujuan.LayananId != "-"
                && !order.LayananTujuan.LayananId.Equals(request.LayananAsalId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"LayananAsal '{request.LayananAsalId}' does not match OrderMutasi destination '{order.LayananTujuan.LayananId}'");
            }
        }

        // 4. Create and persist draft model with 10-char legacy ID (NunaId.NewLegacy("MT", 'A'))
        var mutasiId = NunaId.NewLegacy("MT", 'A');
        var model = MutasiBrgModel.Create(
            id: mutasiId,
            date: DateTime.Now,
            asal: asal.ToReff(),
            tujuan: tujuan.ToReff(),
            orderMutasiId: orderMutasiId,
            dispatchNote: request.DispatchNote ?? string.Empty,
            userId: request.UserId ?? string.Empty);

        _mutasiBrgRepo.SaveChanges(model);

        return Task.FromResult(new MutasiBrgCreateDraftResponse(model.MutasiBrgId));
    }
}
