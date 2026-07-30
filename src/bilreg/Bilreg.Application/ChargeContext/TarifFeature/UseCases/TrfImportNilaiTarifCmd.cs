
using Bilreg.Application.ChargeContext.TarifFeature;
using MediatR;
using Microsoft.Extensions.Logging;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfImportNilaiTarifCmd(
    string? ImportedBy = null,
    bool IsEmergency = false) : IRequest<TrfImportNilaiTarifResponse>;

public record TrfImportNilaiTarifResponse(
    string Message,
    IReadOnlyList<string> Warnings);

public class TrfImportNilaiTarifHandler : IRequestHandler<TrfImportNilaiTarifCmd, TrfImportNilaiTarifResponse>
{
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly ITarifMigrationGuard _migrationGuard;
    private readonly TarifOperationalGate _operationalGate;
    private readonly ITarifOperationalStateRepo _operationalStateRepo;
    private readonly ITarifMigrationModeResolver _modeResolver;
    private readonly ILogger<TrfImportNilaiTarifHandler> _logger;

    public TrfImportNilaiTarifHandler(
        INilaiTarifRepo nilaiTarifRepo,
        ITarifMigrationGuard migrationGuard,
        TarifOperationalGate operationalGate,
        ITarifOperationalStateRepo operationalStateRepo,
        ITarifMigrationModeResolver modeResolver,
        ILogger<TrfImportNilaiTarifHandler> logger)
    {
        _nilaiTarifRepo = nilaiTarifRepo;
        _migrationGuard = migrationGuard;
        _operationalGate = operationalGate;
        _operationalStateRepo = operationalStateRepo;
        _modeResolver = modeResolver;
        _logger = logger;
    }

    public Task<TrfImportNilaiTarifResponse> Handle(
        TrfImportNilaiTarifCmd request,
        CancellationToken cancellationToken)
    {
        _migrationGuard.EnsureImportAllowed(request.IsEmergency);

        var warnings = new List<string>();
        if (request.IsEmergency && !_modeResolver.AllowRoutineImport())
        {
            warnings.Add(
                "Import darurat di mode PublishPrimary/ImportDeprecated; pastikan publish policy adalah jalur operasional utama.");
            _logger.LogWarning(
                "Emergency NilaiTarif import by {ImportedBy} in mode {Mode}",
                request.ImportedBy ?? "SYSTEM",
                _modeResolver.GetEffectiveMode());
        }

        var started = DateTime.UtcNow;
        _logger.LogInformation("NilaiTarif import started");

        using var gate = _operationalGate.Acquire(TarifOperation.Import);
        using var trans = TransHelper.NewScope();
        try
        {
            _nilaiTarifRepo.Import();

            var importedBy = string.IsNullOrWhiteSpace(request.ImportedBy)
                ? "SYSTEM"
                : request.ImportedBy;
            _operationalStateRepo.RecordImport(importedBy, DateTime.UtcNow);

            _logger.LogInformation(
                "NilaiTarif import succeeded in {ElapsedMs} ms",
                (DateTime.UtcNow - started).TotalMilliseconds);
            
            trans.Complete();

            return Task.FromResult(new TrfImportNilaiTarifResponse(
                "Done",
                warnings));
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
    }
}
