namespace Bilreg.Application.AdmisiRanapContext.DigitalSignFeature;

public record OftaGeneralConsentIngestClientRequest(
    byte[] FileBytes,
    string FileName,
    string RegId,
    string DokumenId,
    string ExternalDocumentId,
    string OfficerRef,
    string SignPositionDesc,
    string? SignTag = null,
    int? SignPosition = null,
    string? DocTypeId = null,
    string? DocName = null);

public record OftaGeneralConsentIngestClientResponse(
    string DocId,
    string DocState,
    string RequestedDocUrl,
    string RegId,
    string DokumenId,
    string ExternalDocumentId,
    bool IsExisting);

public record OftaGeneralConsentExecuteClientRequest(
    string DocId,
    string OfficerRef,
    string? Passphrase = null,
    string? Otp = null);

public record OftaGeneralConsentExecuteClientResponse(
    string DocId,
    string DocState,
    string SignState,
    string SignedDocUrl,
    string OfficerEmail,
    string OfficerName,
    DateTime SignedDate,
    bool IsAlreadySigned);

public interface IOftaGeneralConsentClient
{
    Task<OftaGeneralConsentIngestClientResponse> IngestAsync(
        OftaGeneralConsentIngestClientRequest request,
        CancellationToken cancellationToken = default);

    Task<OftaGeneralConsentExecuteClientResponse> ExecuteAsync(
        OftaGeneralConsentExecuteClientRequest request,
        CancellationToken cancellationToken = default);

    Task<byte[]> DownloadSignedDocAsync(
        string signedDocUrl,
        CancellationToken cancellationToken = default);

    Task<string> ArchiveDocAsync(
        string oftaDocId,
        byte[] pdfBytes,
        string fileName,
        string regId,
        string dokumenId,
        string externalDocumentId,
        CancellationToken cancellationToken = default);
}
