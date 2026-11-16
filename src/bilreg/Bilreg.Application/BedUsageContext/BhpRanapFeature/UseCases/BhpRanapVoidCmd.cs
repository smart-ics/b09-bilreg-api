using Ardalis.GuardClauses;
using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.BhpRanapFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.BhpRanapFeature.UseCases;

public record BhpRanapVoidCmd(
    string BhpRanapId,
    string UserId,
    string VoidReason = "")
    : IRequest<BhpRanapVoidResponse>, IBhpRanapKey;

public record BhpRanapVoidResponse(string BhpRanapId, bool Success);

public class BhpRanapVoidHandler : IRequestHandler<BhpRanapVoidCmd, BhpRanapVoidResponse>
{
    private readonly IBhpRanapRepo _bhpRanapRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly IJurnalRepo _jurnalRepo;
    private readonly IMediator _mediator;
    private readonly ITglJamProvider _tglJamProvider;

    public BhpRanapVoidHandler(
        IBhpRanapRepo bhpRanapRepo,
        ITrsBillingRepo trsBillingRepo,
        ITataRekeningRepo tataRekeningRepo,
        IJurnalRepo jurnalRepo,
        IMediator mediator,
        ITglJamProvider tglJamProvider)
    {
        _bhpRanapRepo = bhpRanapRepo;
        _trsBillingRepo = trsBillingRepo;
        _tataRekeningRepo = tataRekeningRepo;
        _jurnalRepo = jurnalRepo;
        _mediator = mediator;
        _tglJamProvider = tglJamProvider;
    }

    public async Task<BhpRanapVoidResponse> Handle(BhpRanapVoidCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BhpRanapId, nameof(request.BhpRanapId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var maybeBhp = _bhpRanapRepo.LoadEntity(BhpRanapModel.Key(request.BhpRanapId));
        if (!maybeBhp.HasValue)
        {
            throw new KeyNotFoundException($"BhpRanap '{request.BhpRanapId}' not found.");
        }

        var bhp = maybeBhp.Value;
        if (bhp.IsVoid)
        {
            throw new InvalidOperationException($"BhpRanap '{request.BhpRanapId}' sudah dibatalkan sebelumnya.");
        }

        var regKey = RegModel.Key(bhp.RegId);
        var tataRekening = _tataRekeningRepo.LoadEntity(regKey).Match(
            onSome: x => x,
            onNone: () => (TataRekeningModel?)null
        );

        if (tataRekening is not null)
        {
            tataRekening.DeleteBill(bhp.BhpRanapId);
        }

        var occurredAt = _tglJamProvider.Now;
        var voidTrsReffId = bhp.BhpRanapId.Length <= 8
            ? $"{bhp.BhpRanapId}_V"
            : $"{bhp.BhpRanapId[..8]}_V";

        var voidCmd = new PostSaleVoidConsequenceCommand(
            bhp.BhpRanapId,
            voidTrsReffId,
            occurredAt,
            request.UserId);

        using var trans = TransHelper.NewScope();

        var voidResult = await _mediator.Send(voidCmd, cancellationToken);
        if (voidResult.Outcome != PostSaleVoidOutcomeEnum.Success && voidResult.Outcome != PostSaleVoidOutcomeEnum.Idempotent)
        {
            throw new InvalidOperationException(
                $"Pengembalian stok gagal ({voidResult.Outcome}). Alasan: {voidResult.InconsistencyReason}");
        }

        bhp.Void(request.UserId, occurredAt, request.VoidReason);
        _bhpRanapRepo.SaveChanges(bhp);

        if (tataRekening is not null)
        {
            _tataRekeningRepo.SaveChanges(tataRekening);
        }

        _trsBillingRepo.DeleteEntity(TrsBillType.Key(bhp.BhpRanapId));
        _jurnalRepo.DeleteEntity(JurnalType.Key(bhp.BhpRanapId));

        trans.Complete();

        return new BhpRanapVoidResponse(bhp.BhpRanapId, true);
    }
}
