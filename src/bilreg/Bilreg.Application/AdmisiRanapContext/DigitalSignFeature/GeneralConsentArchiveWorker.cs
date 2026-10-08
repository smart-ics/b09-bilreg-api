using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.DigitalSignFeature;

public record GeneralConsentArchiveResult(
    string SigningRequestId,
    string RegId,
    string DokumenId,
    string OftaDocId,
    bool Success,
    bool IsAlreadyArchived,
    string ArchiveId,
    string? ErrorMessage = null);

public record GeneralConsentArchiveBatchResult(
    int ProcessedCount,
    int SucceededCount,
    int FailedCount,
    int SkippedCount,
    IEnumerable<GeneralConsentArchiveResult> Details);

public class GeneralConsentArchiveWorker
{
    private readonly IRanapDigitalSignRepo _digitalSignRepo;
    private readonly IOftaGeneralConsentClient _oftaClient;
    private readonly IPatientSignedDocumentRetrievalService _retrievalService;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly ILogger<GeneralConsentArchiveWorker> _logger;

    public GeneralConsentArchiveWorker(
        IRanapDigitalSignRepo digitalSignRepo,
        IOftaGeneralConsentClient oftaClient,
        IPatientSignedDocumentRetrievalService retrievalService,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider,
        ILogger<GeneralConsentArchiveWorker>? logger = null)
    {
        _digitalSignRepo = digitalSignRepo;
        _oftaClient = oftaClient;
        _retrievalService = retrievalService;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
        _logger = logger ?? NullLogger<GeneralConsentArchiveWorker>.Instance;
    }

    public async Task<GeneralConsentArchiveResult> ProcessOneAsync(
        string signingRequestId,
        string triggerType = "WORKER",
        string userId = "SYSTEM",
        CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(signingRequestId);

        var modelMayBe = _digitalSignRepo.LoadEntity(RanapDigitalSignModel.Key(signingRequestId));
        if (!modelMayBe.HasValue)
        {
            _logger.LogWarning("Archive skipped: SigningRequest {Id} not found", signingRequestId);
            return new GeneralConsentArchiveResult(
                SigningRequestId: signingRequestId,
                RegId: string.Empty,
                DokumenId: string.Empty,
                OftaDocId: string.Empty,
                Success: false,
                IsAlreadyArchived: false,
                ArchiveId: string.Empty,
                ErrorMessage: $"SigningRequest '{signingRequestId}' not found");
        }

        var model = modelMayBe.Value;
        return await ProcessModelAsync(model, triggerType, userId, cancellationToken);
    }

    public async Task<GeneralConsentArchiveResult> ProcessCorrelationAsync(
        string regId,
        string dokumenId,
        string triggerType = "ON_DEMAND",
        string userId = "SYSTEM",
        CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(regId);
        Guard.Against.NullOrWhiteSpace(dokumenId);

        var modelMayBe = _digitalSignRepo.LoadByRegDokumen(regId.Trim(), dokumenId.Trim());
        if (!modelMayBe.HasValue)
        {
            return new GeneralConsentArchiveResult(
                SigningRequestId: string.Empty,
                RegId: regId,
                DokumenId: dokumenId,
                OftaDocId: string.Empty,
                Success: false,
                IsAlreadyArchived: false,
                ArchiveId: string.Empty,
                ErrorMessage: $"General Consent correlation for RegId '{regId}' and DokumenId '{dokumenId}' not found");
        }

        return await ProcessModelAsync(modelMayBe.Value, triggerType, userId, cancellationToken);
    }

    public async Task<GeneralConsentArchiveBatchResult> ProcessBatchAsync(
        int batchSize = 50,
        string triggerType = "SCHEDULED",
        string userId = "SYSTEM",
        CancellationToken cancellationToken = default)
    {
        var pendingItems = _digitalSignRepo.ListPendingArchive(batchSize).ToList();
        var results = new List<GeneralConsentArchiveResult>();
        int succeeded = 0;
        int failed = 0;
        int skipped = 0;

        foreach (var item in pendingItems)
        {
            var res = await ProcessModelAsync(item, triggerType, userId, cancellationToken);
            results.Add(res);
            if (res.Success)
            {
                if (res.IsAlreadyArchived) skipped++;
                else succeeded++;
            }
            else
            {
                failed++;
            }
        }

        return new GeneralConsentArchiveBatchResult(
            ProcessedCount: pendingItems.Count,
            SucceededCount: succeeded,
            FailedCount: failed,
            SkippedCount: skipped,
            Details: results);
    }

    private async Task<GeneralConsentArchiveResult> ProcessModelAsync(
        RanapDigitalSignModel model,
        string triggerType,
        string userId,
        CancellationToken cancellationToken)
    {
        // 1. IDEMPOTENCY CHECK: If already archived, return existing archiveId immediately
        if (model.IsArchived && !string.IsNullOrWhiteSpace(model.ArchiveId))
        {
            _logger.LogInformation(
                "General Consent archive skipped (already archived): SigningRequestId={SigningRequestId}, RegId={RegId}, ArchiveId={ArchiveId}",
                model.SigningRequestId, model.RegId, model.ArchiveId);

            return new GeneralConsentArchiveResult(
                SigningRequestId: model.SigningRequestId,
                RegId: model.RegId,
                DokumenId: model.DokumenId,
                OftaDocId: model.OftaDocId,
                Success: true,
                IsAlreadyArchived: true,
                ArchiveId: model.ArchiveId);
        }

        // 2. TERMINAL ELIGIBILITY CHECK: Must be Lengkap (both Officer Signed AND Patient Signed)
        var isOfficerSigned = string.Equals(model.OftaSignState?.Trim(), "Signed", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(model.OftaSignState?.Trim(), "SIGNED", StringComparison.OrdinalIgnoreCase);
        var isPatientSigned = string.Equals(model.PatientSignState?.Trim(), "Signed", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(model.PatientSignState?.Trim(), "SIGNED", StringComparison.OrdinalIgnoreCase);

        if (!isOfficerSigned || !isPatientSigned)
        {
            var msg = $"Correlation is not fully signed (CombinedStatus: {model.CombinedStatus}, Officer: {model.OftaSignState}, Patient: {model.PatientSignState}). Archive suppressed.";
            _logger.LogInformation(
                "Archive suppressed for SigningRequestId={SigningRequestId}, RegId={RegId}: {Message}",
                model.SigningRequestId, model.RegId, msg);

            return new GeneralConsentArchiveResult(
                SigningRequestId: model.SigningRequestId,
                RegId: model.RegId,
                DokumenId: model.DokumenId,
                OftaDocId: model.OftaDocId,
                Success: false,
                IsAlreadyArchived: false,
                ArchiveId: string.Empty,
                ErrorMessage: msg);
        }

        // 3. RETRIEVAL OF COMPLETE TWO-SIGNATURE PDF
        byte[] pdfBytes;
        try
        {
            pdfBytes = await _retrievalService.RetrievePatientSignedPdfAsync(
                model.SigningRequestId,
                model.SignedDocUrl,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve complete signed PDF for SigningRequestId={SigningRequestId}, RegId={RegId}",
                model.SigningRequestId, model.RegId);

            return new GeneralConsentArchiveResult(
                SigningRequestId: model.SigningRequestId,
                RegId: model.RegId,
                DokumenId: model.DokumenId,
                OftaDocId: model.OftaDocId,
                Success: false,
                IsAlreadyArchived: false,
                ArchiveId: string.Empty,
                ErrorMessage: $"PDF retrieval failed: {ex.Message}");
        }

        // 4. REQUEST OFTA ARCHIVE
        string archiveId;
        var now = _tglJamProvider.Now;
        try
        {
            archiveId = await _oftaClient.ArchiveDocAsync(
                oftaDocId: !string.IsNullOrWhiteSpace(model.OftaDocId) ? model.OftaDocId : model.SigningRequestId,
                pdfBytes: pdfBytes,
                fileName: model.FileName,
                regId: model.RegId,
                dokumenId: model.DokumenId,
                externalDocumentId: model.ExternalDocumentId,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OFTA Archive request failed for SigningRequestId={SigningRequestId}, RegId={RegId}",
                model.SigningRequestId, model.RegId);

            return new GeneralConsentArchiveResult(
                SigningRequestId: model.SigningRequestId,
                RegId: model.RegId,
                DokumenId: model.DokumenId,
                OftaDocId: model.OftaDocId,
                Success: false,
                IsAlreadyArchived: false,
                ArchiveId: string.Empty,
                ErrorMessage: $"OFTA archive failed: {ex.Message}");
        }

        // 5. UPDATE PERSISTENCE (Idempotently save archive status)
        var updatedModel = model.SetArchiveStatus(archiveId, userId, now);
        _digitalSignRepo.SaveChanges(updatedModel);

        // 6. AUDIT (No secrets or PDF bytes)
        _auditRepo.SaveChanges(AuditLog.Create(
            updatedModel.AuditTrail.Modified,
            "GENERAL_CONSENT_ARCHIVE",
            nameof(RanapDigitalSignModel),
            $"{model.SigningRequestId}|Trigger:{triggerType}|ArchiveId:{archiveId}|RegId:{model.RegId}|DokumenId:{model.DokumenId}"));

        _logger.LogInformation(
            "General Consent archived successfully: SigningRequestId={SigningRequestId}, RegId={RegId}, ArchiveId={ArchiveId}, Trigger={TriggerType}",
            model.SigningRequestId, model.RegId, archiveId, triggerType);

        return new GeneralConsentArchiveResult(
            SigningRequestId: model.SigningRequestId,
            RegId: model.RegId,
            DokumenId: model.DokumenId,
            OftaDocId: model.OftaDocId,
            Success: true,
            IsAlreadyArchived: false,
            ArchiveId: archiveId);
    }
}
