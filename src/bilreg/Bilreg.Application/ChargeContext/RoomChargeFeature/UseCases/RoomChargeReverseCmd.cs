using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared;
using Bilreg.Application.Shared.BillingLockFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
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
    private readonly IBusinessDateStatus? _businessDateStatus;
    private readonly ITataRekeningRepo? _tataRekeningRepo;

    public RoomChargeReverseHandler(
        IRoomChargeRepo roomChargeRepo,
        IBillingLockValidationService billingLockValidator,
        ITrsBillingRepo trsBillingRepo,
        ITglJamProvider tglJamProvider,
        IBusinessDateStatus? businessDateStatus = null,
        ITataRekeningRepo? tataRekeningRepo = null)
    {
        _roomChargeRepo = roomChargeRepo;
        _billingLockValidator = billingLockValidator;
        _trsBillingRepo = trsBillingRepo;
        _tglJamProvider = tglJamProvider;
        _businessDateStatus = businessDateStatus;
        _tataRekeningRepo = tataRekeningRepo;
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

        // 2. Validates status is not already reversed
        if (sourceCharge.RoomChargeStatus == RoomChargeStatusEnum.Reversed)
        {
            throw new InvalidOperationException(
                $"Cannot reverse room charge '{sourceCharge.RoomChargeId}' because it has already been reversed.");
        }

        var occurredAt = _businessDateStatus?.BusinessNow ?? _tglJamProvider.Now;

        // 3. Deletes room charge with void snapshot log and cleans up billing projections
        var rawId = string.IsNullOrWhiteSpace(sourceCharge.BillingItemRefId)
            ? sourceCharge.RoomChargeId
            : sourceCharge.BillingItemRefId;
        var billingId = rawId.Length > 15 ? rawId[..15] : rawId;

        using (var trans = TransHelper.NewScope())
        {
            _roomChargeRepo.Delete(sourceCharge, request.UserId, occurredAt);

            if (_tataRekeningRepo != null)
            {
                var tataRekening = _tataRekeningRepo.LoadEntity(RegModel.Key(sourceCharge.RegId))
                    .Match<TataRekeningModel?>(
                        onSome: x => x,
                        onNone: () => null
                    );

                if (tataRekening != null)
                {
                    var matchedBill = tataRekening.ListTrsBill
                        .FirstOrDefault(x => string.Equals(x.TrsBillingId, billingId, StringComparison.OrdinalIgnoreCase)
                                          || string.Equals(x.TrsBillingId, rawId, StringComparison.OrdinalIgnoreCase)
                                          || string.Equals(x.TrsBillingId, sourceCharge.RoomChargeId, StringComparison.OrdinalIgnoreCase)
                                          || x.TrsBillingId.StartsWith(billingId, StringComparison.OrdinalIgnoreCase)
                                          || (billingId.Length > 0 && billingId.StartsWith(x.TrsBillingId, StringComparison.OrdinalIgnoreCase)));

                    if (matchedBill != null)
                    {
                        tataRekening.DeleteBill(matchedBill.TrsBillingId);
                        _tataRekeningRepo.SaveChanges(tataRekening);
                    }
                    else
                    {
                        tataRekening.EnsureCanDeleteBill();
                    }
                }
            }

            try
            {
                _trsBillingRepo.DeleteEntity(TrsBillType.Key(billingId));
                if (!string.Equals(rawId, billingId, StringComparison.OrdinalIgnoreCase))
                {
                    _trsBillingRepo.DeleteEntity(TrsBillType.Key(rawId));
                }
                if (!string.Equals(sourceCharge.RoomChargeId, billingId, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(sourceCharge.RoomChargeId, rawId, StringComparison.OrdinalIgnoreCase))
                {
                    _trsBillingRepo.DeleteEntity(TrsBillType.Key(sourceCharge.RoomChargeId));
                }
            }
            catch
            {
                // In case billing ledger item projection encounters legacy schema constraints or was already deleted
            }
            trans.Complete();
        }

        return new RoomChargeReverseResponse(sourceCharge.RoomChargeId, string.Empty);
    }
}
