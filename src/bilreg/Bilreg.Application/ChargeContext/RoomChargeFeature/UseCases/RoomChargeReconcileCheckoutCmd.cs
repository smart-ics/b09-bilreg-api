using Ardalis.GuardClauses;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;

public record RoomChargeReconcileCheckoutCmd(
    string PakaiBedId, 
    DateTime CheckOutTime, 
    string UserId
) : IRequest<RoomChargeReconcileResponse>;

public record RoomChargeReconcileResponse(
    string? RoomChargeId, 
    decimal ChargedUnits, 
    decimal TotalAmount
);

public class RoomChargeReconcileCheckoutHandler : IRequestHandler<RoomChargeReconcileCheckoutCmd, RoomChargeReconcileResponse>
{
    private readonly IRoomChargeRepo _roomChargeRepo;
    private readonly IBillingLockChecker _billingLockChecker;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IBedOccupancyQuery _bedOccupancyQuery;
    private readonly IRoomChargeTarifService _tarifService;
    private readonly ITglJamProvider _tglJamProvider;

    public RoomChargeReconcileCheckoutHandler(
        IRoomChargeRepo roomChargeRepo,
        IBillingLockChecker billingLockChecker,
        ITrsBillingRepo trsBillingRepo,
        IBedOccupancyQuery bedOccupancyQuery,
        IRoomChargeTarifService tarifService,
        ITglJamProvider tglJamProvider)
    {
        _roomChargeRepo = roomChargeRepo;
        _billingLockChecker = billingLockChecker;
        _trsBillingRepo = trsBillingRepo;
        _bedOccupancyQuery = bedOccupancyQuery;
        _tarifService = tarifService;
        _tglJamProvider = tglJamProvider;
    }

    public Task<RoomChargeReconcileResponse> Handle(RoomChargeReconcileCheckoutCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PakaiBedId, nameof(request.PakaiBedId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        cancellationToken.ThrowIfCancellationRequested();

        var occupant = _bedOccupancyQuery.GetOccupant(request.PakaiBedId);
        if (occupant is null)
        {
            throw new KeyNotFoundException($"PakaiBed '{request.PakaiBedId}' not found.");
        }

        // 1. Check billing lock
        _billingLockChecker.AssertBillingNotFinal(occupant.RegId);

        // 2. Calculate remaining duration and grace period via RoomChargeCalculationPolicy
        var cutOffTime = new DateTime(
            request.CheckOutTime.Year,
            request.CheckOutTime.Month,
            request.CheckOutTime.Day,
            12, 0, 0,
            request.CheckOutTime.Kind);

        var chargedUnits = RoomChargeCalculationPolicy.EvaluateDuration(
            occupant.CheckInTime,
            request.CheckOutTime,
            cutOffTime);

        if (occupant.IsRoomingIn)
        {
            chargedUnits = 0.0m;
        }

        // If no units to charge (within <=2h grace period or rooming in)
        if (chargedUnits <= 0m)
        {
            return Task.FromResult(new RoomChargeReconcileResponse(null, 0m, 0m));
        }

        // 3. Evaluate class disparity & tariff
        var (billingClassId, isTitipKelas, isNaikKelas, differentialAmount) =
            RoomChargeCalculationPolicy.EvaluateClassDisparity(
                occupant.PhysicalClassId,
                occupant.PayerClassId,
                occupant.BaseTariff,
                occupant.PayerTariff,
                occupant.IsNaikKelas);

        var tariffResult = _tarifService.GetTarif(billingClassId, occupant.TipeJaminanId);
        var baseTariff = tariffResult.BaseTariff > 0 ? tariffResult.BaseTariff : occupant.BaseTariff;
        var totalAmount = RoomChargeCalculationPolicy.CalculateTotalAmount(baseTariff, chargedUnits, 0m, occupant.IsRoomingIn);

        var scaledKomponen = tariffResult.KomponenList.Select(k => new RoomChargeKomponenModel(
            k.KomponenId,
            k.KomponenName,
            k.KomponenCategory,
            k.Tarif * chargedUnits,
            k.Diskon * chargedUnits,
            k.Total * chargedUnits,
            k.RekeningPendapatan
        )).ToList();

        var now = _tglJamProvider.Now;
        var cycleDate = request.CheckOutTime.ToString("yyyy-MM-dd");

        var roomCharge = RoomChargeModel.Create(
            pakaiBedId: occupant.PakaiBedId,
            regId: occupant.RegId,
            pasienId: occupant.PasienId,
            bangsalId: occupant.BangsalId,
            kamarId: occupant.KamarId,
            bedId: occupant.BedId,
            chargeCycleDate: cycleDate,
            timeCharge: request.CheckOutTime,
            chargeTriggerType: ChargeTriggerTypeEnum.CheckoutReconciliation,
            physicalClassId: occupant.PhysicalClassId,
            payerClassId: occupant.PayerClassId,
            billingClassId: billingClassId,
            isTitipKelas: isTitipKelas,
            isNaikKelas: isNaikKelas,
            differentialAmount: differentialAmount * chargedUnits,
            isRoomingIn: occupant.IsRoomingIn,
            motherPakaiBedId: occupant.MotherPakaiBedId,
            chargedUnits: chargedUnits,
            baseTariff: baseTariff,
            discountAmount: 0m,
            totalAmount: totalAmount,
            listKomponen: scaledKomponen,
            userId: request.UserId,
            occurredAt: now);

        var trsBill = RoomChargeBillingMapper.ToTrsBill(roomCharge);

        using (var trans = TransHelper.NewScope())
        {
            _roomChargeRepo.SaveChanges(roomCharge);
            _trsBillingRepo.SaveChanges(trsBill);
            trans.Complete();
        }

        return Task.FromResult(new RoomChargeReconcileResponse(roomCharge.RoomChargeId, chargedUnits, totalAmount));
    }
}
