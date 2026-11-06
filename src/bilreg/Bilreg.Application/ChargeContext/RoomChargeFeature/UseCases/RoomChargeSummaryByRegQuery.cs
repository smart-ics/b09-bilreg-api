using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.Shared;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;

public record RoomChargeSummaryByRegQuery(string RegId) : IRequest<RoomChargeSummaryView>;

public class RoomChargeSummaryByRegHandler : IRequestHandler<RoomChargeSummaryByRegQuery, RoomChargeSummaryView>
{
    private readonly IRoomChargeRepo _roomChargeRepo;
    private readonly IRegRepo? _regRepo;
    private readonly IBusinessDateStatus? _businessDateStatus;

    public RoomChargeSummaryByRegHandler(
        IRoomChargeRepo roomChargeRepo,
        IRegRepo? regRepo = null,
        IBusinessDateStatus? businessDateStatus = null)
    {
        _roomChargeRepo = roomChargeRepo;
        _regRepo = regRepo;
        _businessDateStatus = businessDateStatus;
    }

    public Task<RoomChargeSummaryView> Handle(RoomChargeSummaryByRegQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));
        cancellationToken.ThrowIfCancellationRequested();

        var list = _roomChargeRepo.ListDataByReg(request.RegId).ToList();

        var tglMasukStr = ResolveAdmissionDate(request.RegId);
        var businessDateStr = ResolveBusinessDate();

        var effectiveTglMasukStr = string.CompareOrdinal(tglMasukStr, businessDateStr) <= 0
            ? tglMasukStr
            : "1900-01-01";

        var filteredList = list.Where(model =>
            !string.IsNullOrWhiteSpace(model.ChargeCycleDate) &&
            string.CompareOrdinal(model.ChargeCycleDate, effectiveTglMasukStr) >= 0 &&
            string.CompareOrdinal(model.ChargeCycleDate, businessDateStr) <= 0
        ).ToList();

        var activeList = filteredList
            .Where(x => x.RoomChargeStatus != RoomChargeStatusEnum.Reversed)
            .ToList();

        var totalChargedUnits = activeList.Sum(x => x.ChargedUnits);
        var totalAmount = activeList.Sum(x => x.TotalAmount);
        var differentialAmount = activeList.Sum(x => x.DifferentialAmount);

        var totalSubsidi = activeList
            .Where(x => x.IsTitipKelas)
            .Sum(x => Math.Max(0m, (x.BaseTariff * x.ChargedUnits) - x.TotalAmount));

        if (totalSubsidi == 0m)
        {
            totalSubsidi = activeList
                .Where(x => x.IsTitipKelas)
                .Sum(x => x.BaseTariff * x.ChargedUnits);
        }

        var activeDaysCount = activeList
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

    private string ResolveAdmissionDate(string regId)
    {
        if (_regRepo is null)
            return "1900-01-01";

        var regOpt = _regRepo.LoadEntity(RegModel.Key(regId));
        return regOpt.Match(
            onSome: reg =>
            {
                if (reg.RegDate != new DateOnly(3000, 1, 1) && reg.RegDate != DateOnly.MinValue)
                    return reg.RegDate.ToString("yyyy-MM-dd");

                if (reg.RegMasukAudit != null &&
                    reg.RegMasukAudit.Timestamp != DateTime.MinValue &&
                    reg.RegMasukAudit.Timestamp != new DateTime(3000, 1, 1))
                {
                    return reg.RegMasukAudit.Timestamp.ToString("yyyy-MM-dd");
                }

                return "1900-01-01";
            },
            onNone: () => "1900-01-01"
        );
    }

    private string ResolveBusinessDate()
    {
        var businessNow = _businessDateStatus?.BusinessNow ?? DateTime.Today;
        return businessNow.ToString("yyyy-MM-dd");
    }
}
