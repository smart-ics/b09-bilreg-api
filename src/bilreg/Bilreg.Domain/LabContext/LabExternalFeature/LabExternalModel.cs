using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.LabContext.LabExternalFeature;

public class LabExternalModel : ILabExternalKey
{
    public const string IdPrefix = "LX";
    public static readonly DateTime EmptyDate = new(3000, 1, 1);

    public string RegExternalId { get; private set; }
    public string ExternalMrNo { get; private set; }
    public LabExternalDemographicsType Demographics { get; private set; }
    public LabExternalReferralInfoType ReferralInfo { get; private set; }
    public string BillingAccountId { get; private set; }
    public LabExternalStatusEnum Status { get; private set; }
    public LabExternalCancelInfoType CancelInfo { get; private set; }
    public AuditTrailType AuditTrail { get; private set; }

    private LabExternalModel(
        string regExternalId,
        string externalMrNo,
        LabExternalDemographicsType demographics,
        LabExternalReferralInfoType referralInfo,
        string billingAccountId,
        LabExternalStatusEnum status,
        LabExternalCancelInfoType cancelInfo,
        AuditTrailType auditTrail)
    {
        RegExternalId = regExternalId;
        ExternalMrNo = externalMrNo;
        Demographics = demographics;
        ReferralInfo = referralInfo;
        BillingAccountId = billingAccountId;
        Status = status;
        CancelInfo = cancelInfo;
        AuditTrail = auditTrail;
    }

    public static LabExternalModel Default => new(
        string.Empty,
        string.Empty,
        LabExternalDemographicsType.Default,
        LabExternalReferralInfoType.Default,
        string.Empty,
        LabExternalStatusEnum.Active,
        LabExternalCancelInfoType.Default,
        AuditTrailType.Default);

    private sealed class LabExternalKey : ILabExternalKey
    {
        public LabExternalKey(string regExternalId) => RegExternalId = regExternalId;
        public string RegExternalId { get; }
    }

    public static ILabExternalKey Key(string regExternalId) => new LabExternalKey(regExternalId);

    // Factory Method untuk Registrasi Baru
    public static LabExternalModel Create(
        string regExternalId,
        string externalMrNo,
        LabExternalDemographicsType demographics,
        LabExternalReferralInfoType referralInfo,
        string billingAccountId,
        AuditInfoType audit)
    {
        Guard.Against.NullOrWhiteSpace(regExternalId, nameof(regExternalId));
        Guard.Against.NullOrWhiteSpace(externalMrNo, nameof(externalMrNo));
        Guard.Against.Null(demographics, nameof(demographics));

        if (string.IsNullOrWhiteSpace(demographics.PatientName))
            throw new ArgumentException("Nama pasien wajib diisi.", nameof(demographics));

        if (string.IsNullOrWhiteSpace(demographics.Gender))
            throw new ArgumentException("Jenis kelamin wajib diisi.", nameof(demographics));

        if (demographics.BirthDate == default || demographics.BirthDate == EmptyDate || demographics.BirthDate > DateTime.Today.AddDays(1))
            throw new ArgumentException("Tanggal lahir pasien tidak valid.", nameof(demographics));

        Guard.Against.Null(referralInfo, nameof(referralInfo));
        Guard.Against.Null(audit, nameof(audit));
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));

        var actualBillingAccountId = string.IsNullOrWhiteSpace(billingAccountId) ? regExternalId : billingAccountId;
        var auditTrail = AuditTrailType.Create(audit.UserId, audit.Timestamp);

        return new LabExternalModel(
            regExternalId,
            externalMrNo,
            demographics,
            referralInfo,
            actualBillingAccountId,
            LabExternalStatusEnum.Active,
            LabExternalCancelInfoType.Default,
            auditTrail);
    }

    // Factory Method untuk Rehidrasi DAL
    public static LabExternalModel Load(
        string regExternalId,
        string externalMrNo,
        LabExternalDemographicsType demographics,
        LabExternalReferralInfoType referralInfo,
        string billingAccountId,
        LabExternalStatusEnum status,
        LabExternalCancelInfoType cancelInfo,
        AuditTrailType auditTrail)
    {
        return new LabExternalModel(
            regExternalId ?? string.Empty,
            externalMrNo ?? string.Empty,
            demographics ?? LabExternalDemographicsType.Default,
            referralInfo ?? LabExternalReferralInfoType.Default,
            billingAccountId ?? string.Empty,
            status,
            cancelInfo ?? LabExternalCancelInfoType.Default,
            auditTrail ?? AuditTrailType.Default);
    }

    // Domain Invariant: Pembatalan Registrasi
    public void Cancel(
        string reason,
        string userId,
        DateTime cancelledAt,
        bool hasPaidCashier,
        bool hasCollectedSpecimen)
    {
        if (Status == LabExternalStatusEnum.Cancelled)
            throw new InvalidOperationException("Registrasi eksternal sudah dalam status dibatalkan.");

        if (Status == LabExternalStatusEnum.Completed)
            throw new InvalidOperationException("Registrasi eksternal yang sudah selesai tidak dapat dibatalkan.");

        // EX-01 Guard: Penolakan jika sudah ada pembayaran di kasir
        if (hasPaidCashier)
            throw new InvalidOperationException("EX-01: Pembatalan ditolak. Tagihan telah dibayar di kasir. Lakukan void kasir terlebih dahulu.");

        // EX-02 Guard: Penolakan jika spesimen telah diambil
        if (hasCollectedSpecimen)
            throw new InvalidOperationException("EX-02: Pembatalan ditolak. Spesimen laboratorium telah diambil untuk order ini.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Alasan pembatalan wajib diisi.", nameof(reason));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID pembatalan wajib diisi.", nameof(userId));

        Status = LabExternalStatusEnum.Cancelled;
        CancelInfo = new LabExternalCancelInfoType(reason, userId, cancelledAt);
        AuditTrail = AuditTrail.Update(userId, cancelledAt);
    }

    // Domain Invariant: Penyelesaian Registrasi
    public void MarkCompleted(string userId, DateTime completedAt)
    {
        if (Status != LabExternalStatusEnum.Active)
            throw new InvalidOperationException($"Hanya registrasi berstatus Active yang dapat diselesaikan. Status saat ini: {Status}.");

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID penyelesaian wajib diisi.", nameof(userId));

        Status = LabExternalStatusEnum.Completed;
        AuditTrail = AuditTrail.Update(userId, completedAt);
    }
}
