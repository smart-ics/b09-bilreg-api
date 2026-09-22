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
        AuditTrailType auditTrail)
    {
        SigningRequestId = signingRequestId;
        RegId = regId;
        HisReference = hisReference;
        DokumenId = dokumenId;
        Pasien = pasien;
        SignerId = signerId;
        FileName = fileName;
        AuditTrail = auditTrail;
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
        DateTime createdAt = default)
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
            AuditTrailType.Create(auditUserId.Trim(), createdAt == default ? DateTime.Now : createdAt));
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
    public AuditTrailType AuditTrail { get; init; }

    #endregion

    #region BEHAVIOUR

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
