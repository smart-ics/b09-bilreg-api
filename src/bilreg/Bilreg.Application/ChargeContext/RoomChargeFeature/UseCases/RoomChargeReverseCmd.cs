using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared.BillingLockFeature;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;

public record RoomChargeReverseCmd(
    string RoomChargeId, 
    string UserId, 
    string Reason
) : IRequest<RoomChargeReverseResponse>;

public record RoomChargeReverseResponse(
    string ReversedRoomChargeId, 
    string StornoRoomChargeId
);

public class RoomChargeReverseHandler : IRequestHandler<RoomChargeReverseCmd, RoomChargeReverseResponse>
{
    private readonly IRoomChargeRepo _roomChargeRepo;
    private readonly IBillingLockValidationService _billingLockValidator;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public RoomChargeReverseHandler(
        IRoomChargeRepo roomChargeRepo,
        IBillingLockValidationService billingLockValidator,
        ITrsBillingRepo trsBillingRepo,
        ITglJamProvider tglJamProvider)
    {
        _roomChargeRepo = roomChargeRepo;
        _billingLockValidator = billingLockValidator;
        _trsBillingRepo = trsBillingRepo;
        _tglJamProvider = tglJamProvider;
    }

    public async Task<RoomChargeReverseResponse> Handle(RoomChargeReverseCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RoomChargeId, nameof(request.RoomChargeId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));
        cancellationToken.ThrowIfCancellationRequested();

        var sourceCharge = _roomChargeRepo.LoadEntity(request.RoomChargeId)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Room Charge '{request.RoomChargeId}' not found.")
            );

        // 1. Checks billing lock
        await _billingLockValidator.EnsureCanModifyBillingAsync(sourceCharge.RegId, cancellationToken);

        // 2. Validates status is Posted
        if (sourceCharge.RoomChargeStatus != RoomChargeStatusEnum.Posted)
        {
            throw new InvalidOperationException(
                $"Cannot reverse room charge '{sourceCharge.RoomChargeId}' because current status is '{sourceCharge.RoomChargeStatus}', expected 'Posted'.");
        }

        var occurredAt = _tglJamProvider.Now;

        // 3. Creates reversal storno record (which also reverses sourceCharge)
        var stornoRecord = sourceCharge.CreateReversalRecord("", request.UserId, request.Reason, occurredAt);

        // 4. In TransHelper.NewScope(), saves both to _roomChargeRepo and saves storno item into _trsBillingRepo
        var stornoBill = RoomChargeBillingMapper.ToTrsBill(stornoRecord);

        using (var trans = TransHelper.NewScope())
        {
            _roomChargeRepo.SaveChanges(sourceCharge);
            _roomChargeRepo.SaveChanges(stornoRecord);
            _trsBillingRepo.SaveChanges(stornoBill);
            trans.Complete();
        }

        return new RoomChargeReverseResponse(sourceCharge.RoomChargeId, stornoRecord.RoomChargeId);
    }
}
