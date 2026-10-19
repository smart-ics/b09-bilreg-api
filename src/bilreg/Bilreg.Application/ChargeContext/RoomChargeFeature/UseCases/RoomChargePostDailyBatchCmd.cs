using Ardalis.GuardClauses;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;

public record RoomChargePostDailyBatchCmd(
    string? BangsalId, 
    DateTime? CutOffDate, 
    string UserId
) : IRequest<RoomChargeBatchResponse>;

public record RoomChargeBatchResponse(
    int TotalProcessed, 
    int TotalSkipped, 
    int TotalFailed, 
    IEnumerable<string> ProcessedIds
);

public class RoomChargePostDailyBatchHandler : IRequestHandler<RoomChargePostDailyBatchCmd, RoomChargeBatchResponse>
{
    private readonly IRoomChargeRepo _roomChargeRepo;
    private readonly IBillingLockChecker _billingLockChecker;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly IBedOccupancyQuery _bedOccupancyQuery;
    private readonly IRoomChargeTarifService _tarifService;
    private readonly ITglJamProvider _tglJamProvider;

    public RoomChargePostDailyBatchHandler(
        IRoomChargeRepo roomChargeRepo,
        IBillingLockChecker billingLockChecker,
        ITrsBillingRepo trsBillingRepo,
        ITarifRepo tarifRepo,
        IBedOccupancyQuery bedOccupancyQuery,
        IRoomChargeTarifService tarifService,
        ITglJamProvider tglJamProvider)
    {
        _roomChargeRepo = roomChargeRepo;
        _billingLockChecker = billingLockChecker;
        _trsBillingRepo = trsBillingRepo;
        _tarifRepo = tarifRepo;
        _bedOccupancyQuery = bedOccupancyQuery;
        _tarifService = tarifService;
        _tglJamProvider = tglJamProvider;
    }

    public Task<RoomChargeBatchResponse> Handle(RoomChargePostDailyBatchCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        cancellationToken.ThrowIfCancellationRequested();

        var now = _tglJamProvider.Now;
        var cutOffDateTime = request.CutOffDate ?? now;
        var cycleDate = cutOffDateTime.ToString("yyyy-MM-dd");

        var occupants = _bedOccupancyQuery.GetActiveOccupants(request.BangsalId, cutOffDateTime).ToList();

        var totalProcessed = 0;
        var totalSkipped = 0;
        var totalFailed = 0;
        var processedIds = new List<string>();

        foreach (var occupant in occupants)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 1. Idempotency check: if room charge for that PakaiBedId and CutOffDate already exists with status Posted, skip
            var existingCharge = _roomChargeRepo.GetByPakaiBedAndDate(occupant.PakaiBedId, cycleDate);
            if (existingCharge.HasValue && existingCharge.Value.RoomChargeStatus == RoomChargeStatusEnum.Posted)
            {
                totalSkipped++;
                continue;
            }

            // 2. Billing Lock Check (aborts if billing is Final)
            _billingLockChecker.AssertBillingNotFinal(occupant.RegId);

            try
            {
                // 3. Evaluate disparity, duration and tariff
                var (billingClassId, isTitipKelas, isNaikKelas, differentialAmount) =
                    RoomChargeCalculationPolicy.EvaluateClassDisparity(
                        occupant.PhysicalClassId,
                        occupant.PayerClassId,
                        occupant.BaseTariff,
                        occupant.PayerTariff,
                        occupant.IsNaikKelas);

                var tariffResult = _tarifService.GetTarif(billingClassId, occupant.TipeJaminanId);
                var baseTariff = tariffResult.BaseTariff > 0 ? tariffResult.BaseTariff : occupant.BaseTariff;
                var chargedUnits = occupant.IsRoomingIn ? 0.0m : 1.0m;
                var totalAmount = RoomChargeCalculationPolicy.CalculateTotalAmount(baseTariff, chargedUnits, 0m, occupant.IsRoomingIn);

                // 4. Create RoomChargeModel
                var roomCharge = RoomChargeModel.Create(
                    pakaiBedId: occupant.PakaiBedId,
                    regId: occupant.RegId,
                    pasienId: occupant.PasienId,
                    bangsalId: occupant.BangsalId,
                    kamarId: occupant.KamarId,
                    bedId: occupant.BedId,
                    chargeCycleDate: cycleDate,
                    timeCharge: cutOffDateTime,
                    chargeTriggerType: ChargeTriggerTypeEnum.DailyCutOff,
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
                    listKomponen: tariffResult.KomponenList,
                    userId: request.UserId,
                    occurredAt: now);

                // 5. In TransHelper.NewScope(), save room charge and billing item
                var trsBill = RoomChargeBillingMapper.ToTrsBill(roomCharge);

                using (var trans = TransHelper.NewScope())
                {
                    _roomChargeRepo.SaveChanges(roomCharge);
                    _trsBillingRepo.SaveChanges(trsBill);
                    trans.Complete();
                }

                processedIds.Add(roomCharge.RoomChargeId);
                totalProcessed++;
            }
            catch (BillingEpisodeFinalException)
            {
                throw;
            }
            catch
            {
                totalFailed++;
            }
        }

        return Task.FromResult(new RoomChargeBatchResponse(totalProcessed, totalSkipped, totalFailed, processedIds));
    }
}
