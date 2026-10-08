using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.AdmisiRanapContext.DigitalSignFeature;

public record RanapDigitalSignModel : IRanapDigitalSignKey
{
    private const string EMPTY_REF_ID = "-";
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public RanapDigitalSignModel(
        string signingRequestId,
        string regId,
        string hisReference,
        string dokumenId,
        PasienReff pasien,
        string signerId,
        string fileName,
        AuditTrailType auditTrail,
        string patientSignState = "",
        string oftaDocId = "",
        string oftaDocState = "",
        string oftaSignState = "",
        string officerRef = "",
        string officerEmail = "",
        string officerName = "",
        string externalDocumentId = "",
        string signedDocUrl = "",
        bool isArchived = false,
        string archiveId = "",
        DateTime? archiveDate = null)
    {
        SigningRequestId = signingRequestId;
        RegId = regId;
        HisReference = hisReference;
        DokumenId = dokumenId;
        Pasien = pasien;
        SignerId = signerId;
        FileName = fileName;
        AuditTrail = auditTrail;
        PatientSignState = patientSignState;
        OftaDocId = oftaDocId;
        OftaDocState = oftaDocState;
        OftaSignState = oftaSignState;
        OfficerRef = officerRef;
        OfficerEmail = officerEmail;
        OfficerName = officerName;
        ExternalDocumentId = externalDocumentId;
        SignedDocUrl = signedDocUrl;
        IsArchived = isArchived;
        ArchiveId = archiveId;
        ArchiveDate = archiveDate ?? EmptyDate;
    }

    #region CREATION

    public static RanapDigitalSignModel CatatCreated(
        string regId,
        string hisReference,
        string dokumenId,
        string signingRequestId,
        PasienReff pasien,
        string signerId,
        string fileName,
        string auditUserId,
        DateTime createdAt = default,
        string patientSignState = "")
    {
        Guard.Against.NullOrWhiteSpace(regId);
        Guard.Against.NullOrWhiteSpace(dokumenId);
        Guard.Against.NullOrWhiteSpace(signingRequestId);
        Guard.Against.Null(pasien);
        Guard.Against.NullOrWhiteSpace(auditUserId);

        var reg = regId.Trim();
        var dok = dokumenId.Trim();
        var signId = signingRequestId.Trim();
        // hisReference = kunci korelasi HIS, independen dan boleh berbeda dengan RegId.
        var hisRef = (hisReference ?? string.Empty).Trim();
        if (reg == EMPTY_REF_ID)
            throw new ArgumentException("RegId tidak valid.", nameof(regId));
        if (dok == EMPTY_REF_ID)
            throw new ArgumentException("DokumenId tidak valid.", nameof(dokumenId));
        if (signId == EMPTY_REF_ID)
            throw new ArgumentException("SigningRequestId tidak valid.", nameof(signingRequestId));
        if (string.IsNullOrWhiteSpace(hisRef) || hisRef == EMPTY_REF_ID)
            throw new ArgumentException("HisReference tidak valid.", nameof(hisReference));
        if (!Guid.TryParse(signId, out _))
            throw new ArgumentException("SigningRequestId harus UUID.", nameof(signingRequestId));

        return new RanapDigitalSignModel(
            signId,
            reg,
            hisRef,
            dok,
            pasien,
            string.IsNullOrWhiteSpace(signerId) ? string.Empty : signerId.Trim(),
            fileName?.Trim() ?? string.Empty,
            AuditTrailType.Create(auditUserId.Trim(), createdAt == default ? DateTime.Now : createdAt),
            patientSignState: patientSignState?.Trim() ?? string.Empty);
    }

    public static RanapDigitalSignModel CatatOftaProxy(
        string regId,
        string hisReference,
        string dokumenId,
        string signingRequestId,
        PasienReff pasien,
        string signerId,
        string fileName,
        string oftaDocId,
        string oftaDocState,
        string oftaSignState,
        string officerRef,
        string officerEmail,
        string officerName,
        string externalDocumentId,
        string signedDocUrl,
        string auditUserId,
        DateTime createdAt = default,
        string patientSignState = "")
    {
        Guard.Against.NullOrWhiteSpace(regId);
        Guard.Against.NullOrWhiteSpace(dokumenId);
        Guard.Against.NullOrWhiteSpace(signingRequestId);
        Guard.Against.Null(pasien);
        Guard.Against.NullOrWhiteSpace(auditUserId);

        var reg = regId.Trim();
        var dok = dokumenId.Trim();
        var signId = signingRequestId.Trim();
        var hisRef = (hisReference ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(hisRef) || hisRef == EMPTY_REF_ID)
            hisRef = reg;

        if (reg == EMPTY_REF_ID)
            throw new ArgumentException("RegId tidak valid.", nameof(regId));
        if (dok == EMPTY_REF_ID)
            throw new ArgumentException("DokumenId tidak valid.", nameof(dokumenId));
        if (signId == EMPTY_REF_ID)
            throw new ArgumentException("SigningRequestId tidak valid.", nameof(signingRequestId));
        if (!Guid.TryParse(signId, out _))
            throw new ArgumentException("SigningRequestId harus UUID.", nameof(signingRequestId));

        return new RanapDigitalSignModel(
            signId,
            reg,
            hisRef,
            dok,
            pasien,
            string.IsNullOrWhiteSpace(signerId) ? string.Empty : signerId.Trim(),
            fileName?.Trim() ?? string.Empty,
            AuditTrailType.Create(auditUserId.Trim(), createdAt == default ? DateTime.Now : createdAt),
            patientSignState: patientSignState?.Trim() ?? string.Empty,
            oftaDocId: oftaDocId?.Trim() ?? string.Empty,
            oftaDocState: oftaDocState?.Trim() ?? string.Empty,
            oftaSignState: oftaSignState?.Trim() ?? string.Empty,
            officerRef: officerRef?.Trim() ?? string.Empty,
            officerEmail: officerEmail?.Trim() ?? string.Empty,
            officerName: officerName?.Trim() ?? string.Empty,
            externalDocumentId: externalDocumentId?.Trim() ?? string.Empty,
            signedDocUrl: signedDocUrl?.Trim() ?? string.Empty,
            isArchived: false,
            archiveId: string.Empty,
            archiveDate: EmptyDate);
    }

    public static RanapDigitalSignModel Default => new(
        "-",
        "-",
        "-",
        "-",
        new PasienReff("-", "-", new DateOnly(3000, 1, 1), "-"),
        string.Empty,
        string.Empty,
        AuditTrailType.Default);

    public static IRanapDigitalSignKey Key(string id) => Default with { SigningRequestId = id };

    #endregion

    #region PROPERTIES

    public string SigningRequestId { get; init; }
    public string RegId { get; init; }
    public string HisReference { get; init; }
    public string DokumenId { get; init; }
    public PasienReff Pasien { get; init; }
    public string SignerId { get; init; }
    public string FileName { get; init; }
    public string PatientSignState { get; init; }
    public string OftaDocId { get; init; }
    public string OftaDocState { get; init; }
    public string OftaSignState { get; init; }
    public string OfficerRef { get; init; }
    public string OfficerEmail { get; init; }
    public string OfficerName { get; init; }
    public string ExternalDocumentId { get; init; }
    public string SignedDocUrl { get; init; }
    public bool IsArchived { get; init; }
    public string ArchiveId { get; init; }
    public DateTime ArchiveDate { get; init; }
    public AuditTrailType AuditTrail { get; init; }

    public string CombinedStatus => ComputeCombinedStatus(OftaSignState, PatientSignState, OftaDocId);

    #endregion

    #region BEHAVIOUR

    public static string ComputeCombinedStatus(string? oftaSignState, string? patientSignState, string? oftaDocId = null)
    {
        var isOfficerSigned = string.Equals(oftaSignState?.Trim(), "SIGNED", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(oftaSignState?.Trim(), "Signed", StringComparison.OrdinalIgnoreCase);
        var isPatientSigned = string.Equals(patientSignState?.Trim(), "SIGNED", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(patientSignState?.Trim(), "Signed", StringComparison.OrdinalIgnoreCase);

        if (isOfficerSigned && isPatientSigned)
            return "Lengkap";

        return "Sebagian";
    }

    public RanapDigitalSignModel UpdatePatientSigning(
        string signerId,
        string fileName,
        string patientSignState,
        string userId,
        DateTime timestamp)
    {
        Guard.Against.NullOrWhiteSpace(userId);

        var audit = new AuditTrailType(
            AuditTrail.Created,
            AuditTrail.Modified,
            AuditTrail.Voided);
        audit.Modif(userId, timestamp);

        return this with
        {
            SignerId = string.IsNullOrWhiteSpace(signerId) ? SignerId : signerId.Trim(),
            FileName = string.IsNullOrWhiteSpace(fileName) ? FileName : fileName.Trim(),
            PatientSignState = string.IsNullOrWhiteSpace(patientSignState) ? PatientSignState : patientSignState.Trim(),
            AuditTrail = audit
        };
    }

    public RanapDigitalSignModel UpdatePatientSignState(
        string patientSignState,
        string userId,
        DateTime timestamp)
    {
        Guard.Against.NullOrWhiteSpace(userId);

        var audit = new AuditTrailType(
            AuditTrail.Created,
            AuditTrail.Modified,
            AuditTrail.Voided);
        audit.Modif(userId, timestamp);

        return this with
        {
            PatientSignState = string.IsNullOrWhiteSpace(patientSignState) ? PatientSignState : patientSignState.Trim(),
            AuditTrail = audit
        };
    }

    public RanapDigitalSignModel UpdateOftaExecution(
        string oftaDocId,
        string oftaDocState,
        string oftaSignState,
        string officerEmail,
        string officerName,
        string signedDocUrl,
        string userId,
        DateTime timestamp)
    {
        Guard.Against.NullOrWhiteSpace(userId);

        var audit = new AuditTrailType(
            AuditTrail.Created,
            AuditTrail.Modified,
            AuditTrail.Voided);
        audit.Modif(userId, timestamp);

        return this with
        {
            OftaDocId = string.IsNullOrWhiteSpace(oftaDocId) ? OftaDocId : oftaDocId.Trim(),
            OftaDocState = string.IsNullOrWhiteSpace(oftaDocState) ? OftaDocState : oftaDocState.Trim(),
            OftaSignState = string.IsNullOrWhiteSpace(oftaSignState) ? OftaSignState : oftaSignState.Trim(),
            OfficerEmail = string.IsNullOrWhiteSpace(officerEmail) ? OfficerEmail : officerEmail.Trim(),
            OfficerName = string.IsNullOrWhiteSpace(officerName) ? OfficerName : officerName.Trim(),
            SignedDocUrl = string.IsNullOrWhiteSpace(signedDocUrl) ? SignedDocUrl : signedDocUrl.Trim(),
            AuditTrail = audit
        };
    }

    public RanapDigitalSignModel SetArchiveStatus(
        string archiveId,
        string userId,
        DateTime archiveDate)
    {
        Guard.Against.NullOrWhiteSpace(archiveId);
        Guard.Against.NullOrWhiteSpace(userId);

        var audit = new AuditTrailType(
            AuditTrail.Created,
            AuditTrail.Modified,
            AuditTrail.Voided);
        audit.Modif(userId, archiveDate);

        return this with
        {
            IsArchived = true,
            ArchiveId = archiveId.Trim(),
            ArchiveDate = archiveDate,
            AuditTrail = audit
        };
    }

    public RanapDigitalSignModel Batal(string userId, string reason, DateTime timestamp)
    {
        Guard.Against.NullOrWhiteSpace(userId);
        Guard.Against.NullOrWhiteSpace(reason);

        var audit = new AuditTrailType(
            AuditTrail.Created,
            AuditTrail.Modified,
            AuditTrail.Voided);
        audit.Batal(userId, timestamp);
        return this with { AuditTrail = audit };
    }

    #endregion
}
