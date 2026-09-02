using Ardalis.GuardClauses;
using Bilreg.Domain.PaymentContext.DepositFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.DepositFeature.UseCases;

public record DepositVoidCmd(
    string DepositId,
    string UserId) : IRequest, IDepositId;

public class DepositVoidHandler : IRequestHandler<DepositVoidCmd>
{
    private readonly IDepositRepo _depositRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public DepositVoidHandler(IDepositRepo depositRepo, ITglJamProvider tglJamProvider)
    {
        _depositRepo = depositRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(DepositVoidCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.DepositId, nameof(request.DepositId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        using var trans = TransHelper.NewScope();

        var deposit = _depositRepo.LoadEntity(request)
            .GetValueOrThrow($"Deposit {request.DepositId} tidak ditemukan.");

        if (deposit.Audit.IsVoided)
            throw new InvalidOperationException($"Deposit {request.DepositId} sudah dibatalkan.");

        deposit.Audit.Batal(request.UserId, _tglJamProvider.Now);
        _depositRepo.SaveChanges(deposit);

        trans.Complete();
        return Task.CompletedTask;
    }
}