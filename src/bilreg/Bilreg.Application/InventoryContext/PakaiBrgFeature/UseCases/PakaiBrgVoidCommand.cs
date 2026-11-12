using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.InventoryContext.PakaiBrgFeature.UseCases;

public record PakaiBrgVoidCommand(
    string PakaiBrgId,
    string UserId,
    string AlasanVoid
) : IRequest<PakaiBrgVoidResponse>, IPakaiBrgKey;

public record PakaiBrgVoidResponse(
    string PakaiBrgId,
    bool IsVoid,
    DateTime TglVoid,
    string AlasanVoid);

public class PakaiBrgVoidHandler : IRequestHandler<PakaiBrgVoidCommand, PakaiBrgVoidResponse>
{
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IPakaiBrgRepo _pakaiBrgRepo;
    private readonly IMediator _mediator;

    public PakaiBrgVoidHandler(
        ITglJamProvider tglJamProvider,
        IPakaiBrgRepo pakaiBrgRepo,
        IMediator mediator)
    {
        _tglJamProvider = tglJamProvider;
        _pakaiBrgRepo = pakaiBrgRepo;
        _mediator = mediator;
    }

    public async Task<PakaiBrgVoidResponse> Handle(PakaiBrgVoidCommand request, CancellationToken cancellationToken)
    {
        // 1. Guard input
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.PakaiBrgId, nameof(request.PakaiBrgId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.AlasanVoid, nameof(request.AlasanVoid));

        // 2. Load existing aggregate
        var pakaiBrg = _pakaiBrgRepo.LoadEntity(PakaiBrgModel.Key(request.PakaiBrgId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Dokumen pemakaian barang dengan Id '{request.PakaiBrgId}' tidak ditemukan.")
            );

        // 3. Time Authority and domain transition
        var now = _tglJamProvider.Now;
        pakaiBrg.Void(request.UserId, request.AlasanVoid, now);

        // 4. Atomic stock reversal and document state persistence
        using var trans = TransHelper.NewScope();

        var voidConsequenceCmd = new PostInternalConsumptionVoidConsequenceCommand(
            pakaiBrg.PakaiBrgId,
            pakaiBrg.PakaiBrgId,
            now,
            request.UserId);

        var voidResult = await _mediator.Send(voidConsequenceCmd, cancellationToken);

        if (voidResult.Outcome != PostInternalConsumptionVoidOutcomeEnum.Success &&
            voidResult.Outcome != PostInternalConsumptionVoidOutcomeEnum.Idempotent)
        {
            var reason = !string.IsNullOrWhiteSpace(voidResult.InconsistencyReason)
                ? voidResult.InconsistencyReason
                : voidResult.Outcome.ToString();
            throw new InvalidOperationException($"Gagal membatalkan pemakaian barang ({reason}).");
        }

        _pakaiBrgRepo.SaveChanges(pakaiBrg);
        trans.Complete();

        return new PakaiBrgVoidResponse(
            pakaiBrg.PakaiBrgId,
            pakaiBrg.IsVoid,
            pakaiBrg.TglVoid,
            pakaiBrg.AlasanVoid);
    }
}
