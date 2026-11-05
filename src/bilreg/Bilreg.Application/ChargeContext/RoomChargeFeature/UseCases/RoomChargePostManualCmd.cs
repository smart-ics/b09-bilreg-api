using Ardalis.GuardClauses;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;

public record RoomChargePostManualCmd(
    string? PakaiBedId,
    string? RegId,
    DateTime? ChargeDate,
    string UserId,
    string? Notes = null,
    decimal? Rate = null
) : IRequest<RoomChargeView>;

public class RoomChargePostManualHandler : IRequestHandler<RoomChargePostManualCmd, RoomChargeView>
{
    private readonly IRoomChargeRepo _roomChargeRepo;
    private readonly IBillingLockChecker _billingLockChecker;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IBedOccupancyQuery _bedOccupancyQuery;
    private readonly IRoomChargeTarifService _tarifService;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IBusinessDateStatus? _businessDateStatus;

    public RoomChargePostManualHandler(
        IRoomChargeRepo roomChargeRepo,
        IBillingLockChecker billingLockChecker,
        ITrsBillingRepo trsBillingRepo,
        IBedOccupancyQuery bedOccupancyQuery,
        IRoomChargeTarifService tarifService,
        ITglJamProvider tglJamProvider,
        IBusinessDateStatus? businessDateStatus = null)
    {
        _roomChargeRepo = roomChargeRepo;
        _billingLockChecker = billingLockChecker;
        _trsBillingRepo = trsBillingRepo;
        _bedOccupancyQuery = bedOccupancyQuery;
        _tarifService = tarifService;
        _tglJamProvider = tglJamProvider;
        _businessDateStatus = businessDateStatus;
    }

    public Task<RoomChargeView> Handle(RoomChargePostManualCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.PakaiBedId) && string.IsNullOrWhiteSpace(request.RegId))
        {
            throw new ArgumentException("PakaiBedId or RegId must be provided.");
        }

        var businessNow = _businessDateStatus?.BusinessNow ?? _tglJamProvider.Now;
        var now = businessNow;
        var chargeDateTime = request.ChargeDate ?? now;

        // Guard: Tanggal charge manual tidak boleh melebihi business date
        if (chargeDateTime.Date > businessNow.Date)
        {
            throw new InvalidOperationException(
                $"Tanggal charge manual ({chargeDateTime:yyyy-MM-dd}) tidak boleh melebihi business date ({businessNow:yyyy-MM-dd}).");
        }

        // 1. Locate occupant
        BedOccupantItem? occupant = null;
        if (!string.IsNullOrWhiteSpace(request.PakaiBedId))
        {
            occupant = _bedOccupancyQuery.GetOccupant(request.PakaiBedId);
        }

        if (occupant == null && !string.IsNullOrWhiteSpace(request.RegId))
        {
            occupant = _bedOccupancyQuery.GetActiveOccupantByReg(request.RegId);
        }

        if (occupant == null)
        {
            throw new KeyNotFoundException($"Active occupant for PakaiBedId '{request.PakaiBedId}' or RegId '{request.RegId}' not found.");
        }

        // 2. Billing Lock Check
        _billingLockChecker.AssertBillingNotFinal(occupant.RegId);

        var cycleDate = chargeDateTime.ToString("yyyy-MM-dd");

        // 2b. Check if an active room charge already exists for the same cycle date
        RoomChargeModel? existingCharge = null;
        var existingByBed = _roomChargeRepo.GetByPakaiBedAndDate(occupant.PakaiBedId, cycleDate);
        if (existingByBed.HasValue && existingByBed.Value.RoomChargeStatus != RoomChargeStatusEnum.Reversed)
        {
            existingCharge = existingByBed.Value;
        }
        else
        {
            existingCharge = _roomChargeRepo.ListDataByReg(occupant.RegId)
                .FirstOrDefault(x => x.ChargeCycleDate == cycleDate && x.RoomChargeStatus != RoomChargeStatusEnum.Reversed);
        }

        // 3. Evaluate Disparity & Rates
        var (billingClassId, isTitipKelas, isNaikKelas, differentialAmount) =
            RoomChargeCalculationPolicy.EvaluateClassDisparity(
                occupant.PhysicalClassId,
                occupant.PayerClassId,
                occupant.BaseTariff,
                occupant.PayerTariff,
                occupant.IsNaikKelas);

        var tariffResult = _tarifService.GetTarif(billingClassId, occupant.TipeJaminanId);
        var baseTariff = (request.Rate.HasValue && request.Rate.Value > 0)
            ? request.Rate.Value
            : (tariffResult.BaseTariff > 0 ? tariffResult.BaseTariff : occupant.BaseTariff);

        var chargedUnits = occupant.IsRoomingIn ? 0.0m : 1.0m;
        var totalAmount = RoomChargeCalculationPolicy.CalculateTotalAmount(baseTariff, chargedUnits, 0m, occupant.IsRoomingIn);

        var komponenList = tariffResult.KomponenList.ToList();
        if (request.Rate.HasValue && request.Rate.Value > 0 && tariffResult.BaseTariff > 0 && tariffResult.BaseTariff != request.Rate.Value)
        {
            var ratio = request.Rate.Value / tariffResult.BaseTariff;
            komponenList = komponenList.Select(k => RoomChargeKomponenModel.Create(
                k.KomponenId,
                k.KomponenName,
                k.KomponenCategory,
                Math.Round(k.Tarif * ratio, 2),
                Math.Round(k.Diskon * ratio, 2),
                k.RekeningPendapatan
            )).ToList();
        }

        RoomChargeModel roomCharge;
        if (existingCharge != null)
        {
            // If room charge already exists on this date/time, update the existing room charge and sync to billing
            existingCharge.Adjust(
                newChargedUnits: chargedUnits,
                newBaseTariff: baseTariff,
                newDiscountAmount: 0m,
                newTotalAmount: totalAmount,
                newListKomponen: komponenList,
                userId: request.UserId,
                occurredAt: now,
                newTimeCharge: chargeDateTime
            );
            roomCharge = existingCharge;
        }
        else
        {
            // 4. Create RoomChargeModel with ManualAdjustment trigger
            roomCharge = RoomChargeModel.Create(
                pakaiBedId: occupant.PakaiBedId,
                regId: occupant.RegId,
                pasienId: occupant.PasienId,
                bangsalId: occupant.BangsalId,
                kamarId: occupant.KamarId,
                bedId: occupant.BedId,
                chargeCycleDate: cycleDate,
                timeCharge: chargeDateTime,
                chargeTriggerType: ChargeTriggerTypeEnum.ManualAdjustment,
                physicalClassId: occupant.PhysicalClassId,
                payerClassId: occupant.PayerClassId,
                billingClassId: billingClassId,
                isTitipKelas: isTitipKelas,
                isNaikKelas: isNaikKelas,
                differentialAmount: differentialAmount,
                isRoomingIn: occupant.IsRoomingIn,
                motherPakaiBedId: occupant.MotherPakaiBedId,
                chargedUnits: chargedUnits,
                baseTariff: baseTariff,
                discountAmount: 0m,
                totalAmount: totalAmount,
                listKomponen: komponenList,
                userId: request.UserId,
                occurredAt: now);
        }

        // 5. Atomic persistence: dual-write to room charge tables + billing item
        var trsBill = RoomChargeBillingMapper.ToTrsBill(roomCharge);

        using (var trans = TransHelper.NewScope())
        {
            _roomChargeRepo.SaveChanges(roomCharge);
            _trsBillingRepo.SaveChanges(trsBill);
            trans.Complete();
        }

        var view = new RoomChargeView(
            roomCharge.RoomChargeId,
            roomCharge.PakaiBedId,
            roomCharge.RegId,
            roomCharge.PasienId,
            roomCharge.BangsalId,
            roomCharge.KamarId,
            roomCharge.BedId,
            roomCharge.ChargeCycleDate,
            roomCharge.TimeCharge,
            roomCharge.ChargeTriggerType.ToString(),
            roomCharge.PhysicalClassId,
            roomCharge.PayerClassId,
            roomCharge.BillingClassId,
            roomCharge.IsTitipKelas,
            roomCharge.IsNaikKelas,
            roomCharge.DifferentialAmount,
            roomCharge.IsRoomingIn,
            roomCharge.MotherPakaiBedId,
            roomCharge.ChargedUnits,
            roomCharge.BaseTariff,
            roomCharge.DiscountAmount,
            roomCharge.TotalAmount,
            roomCharge.RoomChargeStatus.ToString(),
            roomCharge.BillingItemRefId,
            roomCharge.ReversalOfRoomChargeId,
            roomCharge.ReversalReason,
            roomCharge.BedCode,
            roomCharge.KamarName,
            roomCharge.BangsalName,
            roomCharge.PhysicalClassName,
            roomCharge.PayerClassName,
            roomCharge.BillingClassName
        );

        return Task.FromResult(view);
    }
}
