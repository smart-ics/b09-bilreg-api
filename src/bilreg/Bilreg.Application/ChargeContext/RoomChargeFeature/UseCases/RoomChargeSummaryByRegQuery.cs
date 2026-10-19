using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;

public record RoomChargeSummaryByRegQuery(string RegId) : IRequest<RoomChargeSummaryView>;

public class RoomChargeSummaryByRegHandler : IRequestHandler<RoomChargeSummaryByRegQuery, RoomChargeSummaryView>
{
    private readonly IRoomChargeRepo _roomChargeRepo;

    public RoomChargeSummaryByRegHandler(IRoomChargeRepo roomChargeRepo)
    {
        _roomChargeRepo = roomChargeRepo;
    }

    public Task<RoomChargeSummaryView> Handle(RoomChargeSummaryByRegQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));
        cancellationToken.ThrowIfCancellationRequested();

        var list = _roomChargeRepo.ListDataByReg(request.RegId).ToList();

        var totalChargedUnits = list.Sum(x => x.ChargedUnits);
        var totalAmount = list.Sum(x => x.TotalAmount);
        var differentialAmount = list.Sum(x => x.DifferentialAmount);

        var totalSubsidi = list
            .Where(x => x.IsTitipKelas && x.RoomChargeStatus != RoomChargeStatusEnum.Reversed)
            .Sum(x => Math.Max(0m, (x.BaseTariff * x.ChargedUnits) - x.TotalAmount));

        if (totalSubsidi == 0m)
        {
            totalSubsidi = list
                .Where(x => x.IsTitipKelas && x.RoomChargeStatus != RoomChargeStatusEnum.Reversed)
                .Sum(x => x.BaseTariff * x.ChargedUnits);
        }

        var activeDaysCount = list
            .Where(x => x.RoomChargeStatus == RoomChargeStatusEnum.Posted && x.ChargedUnits > 0)
            .Select(x => x.ChargeCycleDate)
            .Distinct()
            .Count();

        var summary = new RoomChargeSummaryView(
            totalChargedUnits,
            totalAmount,
            differentialAmount,
            totalSubsidi,
            activeDaysCount
        );

        return Task.FromResult(summary);
    }
}
