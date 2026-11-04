using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.Shared;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;

public record RoomChargeListByRegQuery(string RegId) : IRequest<IEnumerable<RoomChargeView>>;

public class RoomChargeListByRegHandler : IRequestHandler<RoomChargeListByRegQuery, IEnumerable<RoomChargeView>>
{
    private readonly IRoomChargeRepo _roomChargeRepo;
    private readonly IRegRepo? _regRepo;
    private readonly IBusinessDateStatus? _businessDateStatus;

    public RoomChargeListByRegHandler(
        IRoomChargeRepo roomChargeRepo,
        IRegRepo? regRepo = null,
        IBusinessDateStatus? businessDateStatus = null)
    {
        _roomChargeRepo = roomChargeRepo;
        _regRepo = regRepo;
        _businessDateStatus = businessDateStatus;
    }

    public Task<IEnumerable<RoomChargeView>> Handle(RoomChargeListByRegQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));
        cancellationToken.ThrowIfCancellationRequested();

        var list = _roomChargeRepo.ListDataByReg(request.RegId);

        var tglMasukStr = ResolveAdmissionDate(request.RegId);
        var businessDateStr = ResolveBusinessDate();

        var effectiveTglMasukStr = string.CompareOrdinal(tglMasukStr, businessDateStr) <= 0
            ? tglMasukStr
            : "1900-01-01";

        var filteredList = list.Where(model =>
            !string.IsNullOrWhiteSpace(model.ChargeCycleDate) &&
            string.CompareOrdinal(model.ChargeCycleDate, effectiveTglMasukStr) >= 0 &&
            string.CompareOrdinal(model.ChargeCycleDate, businessDateStr) <= 0
        );

        var views = filteredList.Select(model => new RoomChargeView(
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
            model.BedCode,
            model.KamarName,
            model.BangsalName,
            model.PhysicalClassName,
            model.PayerClassName,
            model.BillingClassName
        ));

        return Task.FromResult(views);
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
