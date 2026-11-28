using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public class PreOpClearanceModel : IPreOpClearanceKey
{
    private const string SentinelDateText = "3000-01-01";
    private readonly List<PreOpChecklistItemType> _checklistItems;

    public PreOpClearanceModel(
        string clearanceId,
        string scheduleOpId,
        string orderOpId,
        string regId,
        int noUrut,
        PreOpClearanceStatusEnum clearanceStatus,
        IEnumerable<PreOpChecklistItemType>? checklistItems,
        string verifikatorPpaId,
        string dokterPjId,
        DateTime completedAt,
        string dokterOverrideId,
        string alasanOverride,
        string alasanNotCleared,
        AuditTrailType auditTrail)
    {
        ClearanceId = clearanceId ?? string.Empty;
        ScheduleOpId = scheduleOpId ?? string.Empty;
        OrderOpId = orderOpId ?? string.Empty;
        RegId = regId ?? string.Empty;
        NoUrut = noUrut;
        ClearanceStatus = clearanceStatus;
        _checklistItems = checklistItems?.ToList() ?? [];
        VerifikatorPpaId = verifikatorPpaId ?? string.Empty;
        DokterPjId = dokterPjId ?? string.Empty;
        CompletedAt = completedAt;
        DokterOverrideId = dokterOverrideId ?? string.Empty;
        AlasanOverride = alasanOverride ?? string.Empty;
        AlasanNotCleared = alasanNotCleared ?? string.Empty;
        AuditTrail = auditTrail ?? AuditTrailType.Default;
    }

    public static PreOpClearanceModel Default => new(
        "-", "-", "-", "-", 0,
        PreOpClearanceStatusEnum.PendingClearance,
        [], "", "", SentinelDate,
        "", "", "", AuditTrailType.Default);

    public static IPreOpClearanceKey Key(string id) => new PreOpClearanceModel(
        id, "-", "-", "-", 0,
        PreOpClearanceStatusEnum.PendingClearance,
        [], "", "", SentinelDate,
        "", "", "", AuditTrailType.Default);

    public static PreOpClearanceModel Create(
        string scheduleOpId,
        string orderOpId,
        string regId,
        int noUrut,
        string verifikatorPpaId,
        string dokterPjId,
        string userId,
        DateTime createdAt)
    {
        if (noUrut < 1)
            throw new ArgumentOutOfRangeException(nameof(noUrut), "NoUrut harus lebih besar atau sama dengan 1.");

        return new PreOpClearanceModel(
            Ulid.NewUlid().ToString(), scheduleOpId, orderOpId, regId, noUrut,
            PreOpClearanceStatusEnum.PendingClearance, [], verifikatorPpaId,
            dokterPjId, SentinelDate, "", "", "",
            AuditTrailType.Create(userId, createdAt));
    }

    public string ClearanceId { get; init; }
    public string ScheduleOpId { get; init; }
    public string OrderOpId { get; init; }
    public string RegId { get; init; }
    public int NoUrut { get; init; }
    public PreOpClearanceStatusEnum ClearanceStatus { get; private set; }
    public IEnumerable<PreOpChecklistItemType> ChecklistItems => _checklistItems;
    public string VerifikatorPpaId { get; private set; }
    public string DokterPjId { get; private set; }
    public DateTime CompletedAt { get; private set; }
    public string DokterOverrideId { get; private set; }
    public string AlasanOverride { get; private set; }
    public string AlasanNotCleared { get; private set; }
    public AuditTrailType AuditTrail { get; private set; }

    public void UpdateChecklist(IEnumerable<PreOpChecklistItemType> items, string userId, DateTime updatedAt)
    {
        EnsurePending();
        _checklistItems.Clear();
        if (items is not null)
            _checklistItems.AddRange(items);
        AuditTrail.Modif(userId, updatedAt);
    }

    public void Approve(string userId, DateTime clearedAt)
    {
        EnsurePending();
        if (_checklistItems.Count == 0 || _checklistItems.Any(x =>
                x.Status is ChecklistItemStatusEnum.Pending or ChecklistItemStatusEnum.Failed))
            throw new PreOpChecklistIncompleteException();

        ClearanceStatus = PreOpClearanceStatusEnum.Cleared;
        CompletedAt = clearedAt;
        AuditTrail.Modif(userId, clearedAt);
    }

    public void Reject(string alasan, string userId, DateTime rejectedAt)
    {
        EnsurePending();
        if (string.IsNullOrWhiteSpace(alasan))
            throw new ArgumentException("Alasan NOT_CLEARED wajib diisi.", nameof(alasan));

        ClearanceStatus = PreOpClearanceStatusEnum.NotCleared;
        AlasanNotCleared = alasan.Trim();
        CompletedAt = rejectedAt;
        AuditTrail.Modif(userId, rejectedAt);
    }

    public void EmergencyOverride(
        string dokterOverrideId,
        string alasanOverride,
        string userId,
        DateTime overriddenAt,
        UrgencyLevelEnum urgencyLevel,
        IEnumerable<string> authorizedDokterPpaIds)
    {
        EnsurePending();
        if (urgencyLevel != UrgencyLevelEnum.Cito)
            throw new InvalidOperationException("Emergency override hanya diperbolehkan untuk kasus Cito.");
        if (string.IsNullOrWhiteSpace(dokterOverrideId) ||
            !(authorizedDokterPpaIds ?? []).Contains(dokterOverrideId, StringComparer.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Dokter override tidak berwenang untuk clearance ini.");
        if (string.IsNullOrWhiteSpace(alasanOverride) || alasanOverride.Trim().Length < 10)
            throw new ArgumentException("Alasan emergency override minimal 10 karakter.", nameof(alasanOverride));

        ClearanceStatus = PreOpClearanceStatusEnum.EmergencyOverridden;
        DokterOverrideId = dokterOverrideId.Trim();
        AlasanOverride = alasanOverride.Trim();
        CompletedAt = overriddenAt;
        AuditTrail.Modif(userId, overriddenAt);
    }

    private void EnsurePending()
    {
        if (ClearanceStatus != PreOpClearanceStatusEnum.PendingClearance)
            throw new InvalidOperationException(
                $"Clearance tidak dapat diubah setelah berstatus {ClearanceStatus}.");
    }

    private static DateTime SentinelDate => new(3000, 1, 1);
}

public interface IPreOpClearanceKey
{
    string ClearanceId { get; }
}
