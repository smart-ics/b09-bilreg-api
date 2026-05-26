
using MediatR;
using Microsoft.Extensions.Logging;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfImportNilaiTarifCmd() : IRequest;

public class TrfImportNilaiTarifHandler : IRequestHandler<TrfImportNilaiTarifCmd>
{
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly ILogger<TrfImportNilaiTarifHandler> _logger;

    public TrfImportNilaiTarifHandler(
        INilaiTarifRepo nilaiTarifRepo,
        ILogger<TrfImportNilaiTarifHandler> logger)
    {
        _nilaiTarifRepo = nilaiTarifRepo;
        _logger = logger;
    }

    public Task Handle(TrfImportNilaiTarifCmd request, CancellationToken cancellationToken)
    {
        var started = DateTime.UtcNow;
        _logger.LogInformation("NilaiTarif import started");

        using var trans = TransHelper.NewScope();
        try
        {
            _nilaiTarifRepo.Import();
            trans.Complete();
            _logger.LogInformation(
                "NilaiTarif import succeeded in {ElapsedMs} ms",
                (DateTime.UtcNow - started).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "NilaiTarif import failed after {ElapsedMs} ms",
                (DateTime.UtcNow - started).TotalMilliseconds);
            throw new InvalidOperationException(
                "NilaiTarif import failed; BILRG projection unchanged (transaction rolled back).",
                ex);
        }

        return Task.CompletedTask;
    }
}
