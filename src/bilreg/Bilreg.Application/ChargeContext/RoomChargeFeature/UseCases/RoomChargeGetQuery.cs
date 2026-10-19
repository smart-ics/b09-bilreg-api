using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;

public record RoomChargeGetQuery(string RoomChargeId) : IRequest<RoomChargeDetailView>;

public class RoomChargeGetHandler : IRequestHandler<RoomChargeGetQuery, RoomChargeDetailView>
{
    private readonly IRoomChargeRepo _roomChargeRepo;

    public RoomChargeGetHandler(IRoomChargeRepo roomChargeRepo)
    {
        _roomChargeRepo = roomChargeRepo;
    }

    public Task<RoomChargeDetailView> Handle(RoomChargeGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RoomChargeId, nameof(request.RoomChargeId));
        cancellationToken.ThrowIfCancellationRequested();

        var model = _roomChargeRepo.LoadEntity(request.RoomChargeId)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Room Charge '{request.RoomChargeId}' not found.")
            );

        var komponenViews = model.ListKomponen.Select(k => new RoomChargeKomponenView(
            k.KomponenId,
            k.KomponenName,
            k.KomponenCategory,
            k.Tarif,
            k.Diskon,
            k.Total,
            k.RekeningPendapatan
        )).ToList();

        var detailView = new RoomChargeDetailView(
            model.RoomChargeId,
            model.PakaiBedId,
            model.RegId,
            model.PasienId,
            model.BangsalId,
            model.KamarId,
            model.BedId,
            model.ChargeCycleDate,
            model.TimeCharge,
            model.ChargeTriggerType.ToString(),
            model.PhysicalClassId,
            model.PayerClassId,
            model.BillingClassId,
            model.IsTitipKelas,
            model.IsNaikKelas,
            model.DifferentialAmount,
            model.IsRoomingIn,
            model.MotherPakaiBedId,
            model.ChargedUnits,
            model.BaseTariff,
            model.DiscountAmount,
            model.TotalAmount,
            model.RoomChargeStatus.ToString(),
            model.BillingItemRefId,
            model.ReversalOfRoomChargeId,
            model.ReversalReason,
            komponenViews
        );

        return Task.FromResult(detailView);
    }
}
