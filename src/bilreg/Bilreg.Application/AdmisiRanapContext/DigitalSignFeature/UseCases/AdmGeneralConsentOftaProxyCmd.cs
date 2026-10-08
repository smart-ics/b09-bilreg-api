using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.Shared;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.DigitalSignFeature.UseCases;

public record AdmGeneralConsentOftaProxyCmd(
    string RegId,
    string DokumenId,
    string ExternalDocumentId,
    string OfficerRef,
    string SignPositionDesc,
    byte[] FileBytes,
    string FileName,
    string? SignTag = null,
    int? SignPosition = null,
    string? DocTypeId = null,
    string? DocName = null,
    string? Passphrase = null,
    string? Otp = null,
    string? PatientSignerId = null,
    string? SigningRequestId = null,
    string? HisReference = null,
    string UserId = "") : IRequest<AdmGeneralConsentOftaProxyResponse>, IRegKey;

public record AdmGeneralConsentOftaProxyResponse(
    string DocId,
    string DocState,
    string SignState,
    string SignedDocUrl,
    string OfficerEmail,
    string OfficerName,
    DateTime SignedDate,
    string SigningRequestId,
    string RegId,
    string DokumenId,
    string ExternalDocumentId,
    bool IsAlreadySigned,
    string SignedPdfBase64);

public class AdmGeneralConsentOftaProxyHandler
    : IRequestHandler<AdmGeneralConsentOftaProxyCmd, AdmGeneralConsentOftaProxyResponse>
{
    private static readonly DateTime VoidSentinel = new(3000, 1, 1);

    private readonly IRanapDigitalSignRepo _digitalSignRepo;
    private readonly IAdmissionRepo _admissionRepo;
    private readonly IOftaGeneralConsentClient _oftaClient;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public AdmGeneralConsentOftaProxyHandler(
        IRanapDigitalSignRepo digitalSignRepo,
        IAdmissionRepo admissionRepo,
        IOftaGeneralConsentClient oftaClient,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider)
    {
        _digitalSignRepo = digitalSignRepo;
        _admissionRepo = admissionRepo;
        _oftaClient = oftaClient;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public async Task<AdmGeneralConsentOftaProxyResponse> Handle(
        AdmGeneralConsentOftaProxyCmd request,
        CancellationToken cancellationToken)
    {
        // 1. GUARDS
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.DokumenId);
        Guard.Against.NullOrWhiteSpace(request.ExternalDocumentId);
        Guard.Against.NullOrWhiteSpace(request.OfficerRef);
        Guard.Against.NullOrWhiteSpace(request.SignPositionDesc);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        if (request.FileBytes is null || request.FileBytes.Length == 0)
            throw new ArgumentException("PDF file content is required.", nameof(request.FileBytes));

        var regId = request.RegId.Trim();
        var dokumenId = request.DokumenId.Trim();
        var externalDocumentId = request.ExternalDocumentId.Trim();
        var officerRef = request.OfficerRef.Trim();
        var signPositionDesc = request.SignPositionDesc.Trim();
        var hisReference = string.IsNullOrWhiteSpace(request.HisReference) ? regId : request.HisReference.Trim();
        var fileName = string.IsNullOrWhiteSpace(request.FileName) ? "general-consent.pdf" : request.FileName.Trim();

        // 2. LOAD ADMISSION (fail-fast if admission not found)
        var admission = _admissionRepo.LoadEntity(AdmissionModel.Key(regId))
            .GetValueOrThrow($"Admission '{regId}' tidak ditemukan.");

        // 3. ACTIVE CORRELATION GUARD (TD-207)
        var existingByDoc = _digitalSignRepo.LoadByRegDokumen(regId, dokumenId);
        if (existingByDoc.HasValue)
        {
            var existing = existingByDoc.Value;
            var isVoided = existing.AuditTrail.Voided.Timestamp != VoidSentinel;
            if (!isVoided)
            {
                if (!string.IsNullOrWhiteSpace(existing.ExternalDocumentId) &&
                    !string.Equals(existing.ExternalDocumentId, externalDocumentId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Active General Consent for RegId '{regId}' and DokumenId '{dokumenId}' already exists with a different external correlation '{existing.ExternalDocumentId}'.");
                }
            }
        }

        // 4. OFTA INGEST
        var ingestRequest = new OftaGeneralConsentIngestClientRequest(
            FileBytes: request.FileBytes,
            FileName: fileName,
            RegId: regId,
            DokumenId: dokumenId,
            ExternalDocumentId: externalDocumentId,
            OfficerRef: officerRef,
            SignPositionDesc: signPositionDesc,
            SignTag: request.SignTag,
            SignPosition: request.SignPosition,
            DocTypeId: request.DocTypeId,
            DocName: request.DocName);

        var ingestResponse = await _oftaClient.IngestAsync(ingestRequest, cancellationToken);

        // 5. OFTA EXECUTE
        var executeRequest = new OftaGeneralConsentExecuteClientRequest(
            DocId: ingestResponse.DocId,
            OfficerRef: officerRef,
            Passphrase: request.Passphrase,
            Otp: request.Otp);

        var executeResponse = await _oftaClient.ExecuteAsync(executeRequest, cancellationToken);

        // 6. DOWNLOAD SIGNED PDF BYTES
        var signedPdfBytes = await _oftaClient.DownloadSignedDocAsync(executeResponse.SignedDocUrl, cancellationToken);

        // 7. PERSIST ATOMIC CORRELATION
        var signingRequestId = ResolveSigningRequestId(request.SigningRequestId, existingByDoc);
        var pasien = admission.Pasien ?? new PasienReff("-", "-", new DateOnly(3000, 1, 1), "-");
        var now = _tglJamProvider.Now;

        RanapDigitalSignModel modelToSave;
        if (existingByDoc.HasValue)
        {
            var existing = existingByDoc.Value;
            modelToSave = existing.UpdateOftaExecution(
                oftaDocId: executeResponse.DocId,
                oftaDocState: executeResponse.DocState,
                oftaSignState: executeResponse.SignState,
                officerEmail: executeResponse.OfficerEmail,
                officerName: executeResponse.OfficerName,
                signedDocUrl: executeResponse.SignedDocUrl,
                userId: request.UserId.Trim(),
                timestamp: now);
        }
        else
        {
            modelToSave = RanapDigitalSignModel.CatatOftaProxy(
                regId: regId,
                hisReference: hisReference,
                dokumenId: dokumenId,
                signingRequestId: signingRequestId,
                pasien: pasien,
                signerId: request.PatientSignerId ?? string.Empty,
                fileName: fileName,
                oftaDocId: executeResponse.DocId,
                oftaDocState: executeResponse.DocState,
                oftaSignState: executeResponse.SignState,
                officerRef: officerRef,
                officerEmail: executeResponse.OfficerEmail,
                officerName: executeResponse.OfficerName,
                externalDocumentId: externalDocumentId,
                signedDocUrl: executeResponse.SignedDocUrl,
                auditUserId: request.UserId.Trim(),
                createdAt: now);
        }

        _digitalSignRepo.SaveChanges(modelToSave);

        // 8. AUDIT (no secrets logged)
        _auditRepo.SaveChanges(AuditLog.Create(
            modelToSave.AuditTrail.Created,
            "OFTA_EXECUTE_PROXY",
            nameof(RanapDigitalSignModel),
            $"{modelToSave.SigningRequestId}|DocId:{executeResponse.DocId}|SignState:{executeResponse.SignState}"));

        return new AdmGeneralConsentOftaProxyResponse(
            DocId: executeResponse.DocId,
            DocState: executeResponse.DocState,
            SignState: executeResponse.SignState,
            SignedDocUrl: executeResponse.SignedDocUrl,
            OfficerEmail: executeResponse.OfficerEmail,
            OfficerName: executeResponse.OfficerName,
            SignedDate: executeResponse.SignedDate,
            SigningRequestId: modelToSave.SigningRequestId,
            RegId: regId,
            DokumenId: dokumenId,
            ExternalDocumentId: externalDocumentId,
            IsAlreadySigned: executeResponse.IsAlreadySigned,
            SignedPdfBase64: Convert.ToBase64String(signedPdfBytes));
    }

    private static string ResolveSigningRequestId(
        string? requestedSigningRequestId,
        MayBe<RanapDigitalSignModel> existingModel)
    {
        if (existingModel.HasValue && !string.IsNullOrWhiteSpace(existingModel.Value.SigningRequestId) &&
            Guid.TryParse(existingModel.Value.SigningRequestId, out _))
        {
            return existingModel.Value.SigningRequestId;
        }

        if (!string.IsNullOrWhiteSpace(requestedSigningRequestId) &&
            Guid.TryParse(requestedSigningRequestId.Trim(), out var parsedGuid))
        {
            return parsedGuid.ToString("D");
        }

        return Guid.NewGuid().ToString("D");
    }
}
