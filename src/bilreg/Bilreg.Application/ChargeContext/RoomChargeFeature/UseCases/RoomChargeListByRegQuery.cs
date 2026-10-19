using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;

public record RoomChargeListByRegQuery(string RegId) : IRequest<IEnumerable<RoomChargeView>>;

public class RoomChargeListByRegHandler : IRequestHandler<RoomChargeListByRegQuery, IEnumerable<RoomChargeView>>
{
    private readonly IRoomChargeRepo _roomChargeRepo;

    public RoomChargeListByRegHandler(IRoomChargeRepo roomChargeRepo)
    {
        _roomChargeRepo = roomChargeRepo;
    }

    public Task<IEnumerable<RoomChargeView>> Handle(RoomChargeListByRegQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));
        cancellationToken.ThrowIfCancellationRequested();

        var list = _roomChargeRepo.ListDataByReg(request.RegId);

        var views = list.Select(model => new RoomChargeView(
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
            model.ReversalReason
        ));

        return Task.FromResult(views);
    }
}
