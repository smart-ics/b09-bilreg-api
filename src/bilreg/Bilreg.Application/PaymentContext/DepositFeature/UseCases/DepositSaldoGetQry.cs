using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using MediatR;

namespace Bilreg.Application.PaymentContext.DepositFeature.UseCases;

public record DepositSaldoGetQry(string RegId) : IRequest<DepositSaldoResponse>, IRegKey;

public record DepositSaldoResponse(
    string RegId,
    decimal TotalSetoran,
    decimal TotalTerpakai,
    decimal SaldoAktif,
    string Status);

public class DepositSaldoGetHandler : IRequestHandler<DepositSaldoGetQry, DepositSaldoResponse>
{
    private readonly IDepositRepo _depositRepo;

    public DepositSaldoGetHandler(IDepositRepo depositRepo)
    {
        _depositRepo = depositRepo;
    }

    public Task<DepositSaldoResponse> Handle(DepositSaldoGetQry request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));

        var saldo = _depositRepo.GetSaldo(request) ?? new DepositSaldoModel(request.RegId, 0, 0, 0);
        var deposits = (_depositRepo.ListData(request) ?? []).ToList();

        string status;
        if (deposits.Any() && deposits.All(d => d.Audit.IsVoided))
        {
            status = "Dibatalkan";
        }
        else if (saldo.SaldoAktif > 0)
        {
            status = "Aktif";
        }
        else if (saldo.TotalSetoran > 0 && saldo.SaldoAktif <= 0)
        {
            status = "Habis Terpakai";
        }
        else
        {
            status = "Tidak Ada Deposit";
        }

        var result = new DepositSaldoResponse(
            request.RegId,
            saldo.TotalSetoran,
            saldo.TotalTerpakai,
            saldo.SaldoAktif,
            status);

        return Task.FromResult(result);
    }
}
