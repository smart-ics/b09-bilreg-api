using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.LabContext.LabResultFeature;

public class LabResultDocumentModel : ILabResultDocumentKey
{
    private const string IdPrefix = "LRD";
    private static readonly DateTime EmptyDateSentinel = new(3000, 1, 1);
    private readonly List<LabResultItemModel> _items;

    private LabResultDocumentModel(
        string resultDocumentId,
        string orderId,
        int versionNo,
        bool isCurrentVersion,
        LabResultSourceEnum resultSource,
        LabResultStatusEnum resultStatus,
        DateTime recordedDate,
        string recordedUserId,
        DateTime verifiedDate,
        string verifiedUserId,
        string amendmentReason,
        DateTime amendedDate,
        string amendedUserId,
        string previousVersionId,
        AuditTrailType auditTrail,
        IEnumerable<LabResultItemModel> items,
        string expertiseNote = "",
        string criticalNote = "")
    {
        ResultDocumentId = resultDocumentId;
        OrderId = orderId;
        VersionNo = versionNo;
        IsCurrentVersion = isCurrentVersion;
        ResultSource = resultSource;
        ResultStatus = resultStatus;
        RecordedDate = recordedDate;
        RecordedUserId = recordedUserId;
        VerifiedDate = verifiedDate;
        VerifiedUserId = verifiedUserId;
        AmendmentReason = amendmentReason;
        AmendedDate = amendedDate;
        AmendedUserId = amendedUserId;
        PreviousVersionId = previousVersionId;
        AuditTrail = auditTrail;
        _items = items.ToList();
        ExpertiseNote = expertiseNote ?? string.Empty;
        CriticalNote = criticalNote ?? string.Empty;
    }

    public static LabResultDocumentModel Default => new(
        "-",
        "",
        1,
        true,
        LabResultSourceEnum.Manual,
        LabResultStatusEnum.Draft,
        AuditInfoType.Default.Timestamp,
        "",
        AuditInfoType.Default.Timestamp,
        "",
        "",
        EmptyDateSentinel,
        "",
        "",
        AuditTrailType.Default,
        [],
        "",
        "");

    private sealed class ResultDocumentKey : ILabResultDocumentKey
    {
        public ResultDocumentKey(string resultDocumentId) => ResultDocumentId = resultDocumentId;
        public string ResultDocumentId { get; }
    }

    public static ILabResultDocumentKey Key(string resultDocumentId) => new ResultDocumentKey(resultDocumentId);

    public static LabResultDocumentModel Load(
        string resultDocumentId,
        string orderId,
        int versionNo,
        bool isCurrentVersion,
        LabResultSourceEnum resultSource,
        LabResultStatusEnum resultStatus,
        DateTime recordedDate,
        string recordedUserId,
        DateTime verifiedDate,
        string verifiedUserId,
        string amendmentReason,
        DateTime amendedDate,
        string amendedUserId,
        string previousVersionId,
        AuditTrailType auditTrail,
        IEnumerable<LabResultItemModel> items,
        string expertiseNote = "",
        string criticalNote = "")
        => new(
            resultDocumentId,
            orderId,
            versionNo,
            isCurrentVersion,
            resultSource,
            resultStatus,
            recordedDate,
            recordedUserId,
            verifiedDate,
            verifiedUserId,
            amendmentReason,
            amendedDate,
            amendedUserId,
            previousVersionId,
            auditTrail,
            items,
            expertiseNote,
            criticalNote);

    public static LabResultDocumentModel CreateInitial(string orderId, AuditInfoType audit)
    {
        Guard.Against.NullOrWhiteSpace(orderId, nameof(orderId));
        Guard.Against.Null(audit);
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));

        var resultDocumentId = NunaId.New(IdPrefix);
        var auditTrail = AuditTrailType.Create(audit.UserId, audit.Timestamp);

        return new LabResultDocumentModel(
            resultDocumentId,
            orderId,
            versionNo: 1,
            isCurrentVersion: true,
            resultSource: LabResultSourceEnum.Manual,
            resultStatus: LabResultStatusEnum.Draft,
            recordedDate: AuditInfoType.Default.Timestamp,
            recordedUserId: "",
            verifiedDate: AuditInfoType.Default.Timestamp,
            verifiedUserId: "",
            amendmentReason: "",
            amendedDate: EmptyDateSentinel,
            amendedUserId: "",
            previousVersionId: "",
            auditTrail,
            [],
            "",
            "");
    }

    public static LabResultDocumentModel CreateFromLisIngestion(
        string orderId,
        IEnumerable<LabResultItemModel> items,
        string validatorName,
        DateTime validatedAt,
        AuditInfoType audit,
        string criticalNote = "",
        string expertiseNote = "")
    {
        Guard.Against.NullOrWhiteSpace(orderId, nameof(orderId));
        Guard.Against.Null(items, nameof(items));
        Guard.Against.NullOrWhiteSpace(validatorName, nameof(validatorName));
        Guard.Against.Null(audit);
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));

        var itemList = items.ToList();
        var at = validatedAt == default ? audit.Timestamp : validatedAt;
        var resultDocumentId = NunaId.New(IdPrefix);
        var auditTrail = AuditTrailType.Create(audit.UserId, audit.Timestamp);

        var critNote = (criticalNote ?? string.Empty).Trim();
        if (critNote.Length > 1000)
            critNote = critNote[..1000];

        var expNote = (expertiseNote ?? string.Empty).Trim();
        if (expNote.Length > 2000)
            expNote = expNote[..2000];

        return new LabResultDocumentModel(
            resultDocumentId,
            orderId,
            versionNo: 1,
            isCurrentVersion: true,
            resultSource: LabResultSourceEnum.ExternalLis,
            resultStatus: LabResultStatusEnum.Verified,
            recordedDate: at,
            recordedUserId: validatorName,
            verifiedDate: at,
            verifiedUserId: validatorName,
            amendmentReason: string.Empty,
            amendedDate: EmptyDateSentinel,
            amendedUserId: string.Empty,
            previousVersionId: string.Empty,
            auditTrail,
            itemList,
            expNote,
            critNote);
    }

    /// <summary>
    /// Creates a non-current snapshot of this verified/released version and a new current version in Recorded status (re-verification required).
    /// </summary>
    public (LabResultDocumentModel RetiredVersion, LabResultDocumentModel NewVersion) AmendVerifiedToNewVersion(
        IEnumerable<LabResultItemModel> regeneratedScaffoldItems,
        string reason,
        string amendedBy,
        DateTime amendedAt)
    {
        Guard.Against.Null(regeneratedScaffoldItems, nameof(regeneratedScaffoldItems));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
        Guard.Against.NullOrWhiteSpace(amendedBy, nameof(amendedBy));

        var scaffoldItems = regeneratedScaffoldItems.ToList();
        Guard.Against.NullOrEmpty(scaffoldItems, nameof(regeneratedScaffoldItems));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah void; amend tidak diperbolehkan.");

        if (!IsCurrentVersion)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} bukan versi saat ini; amend tidak diperbolehkan.");

        if (ResultStatus != LabResultStatusEnum.Verified && ResultStatus != LabResultStatusEnum.Released)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} berstatus {ResultStatus}; amend hanya diperbolehkan untuk hasil Verified atau Released.");

        var r = reason.Trim();
        if (r.Length == 0)
            throw new ArgumentException("Amendment reason wajib diisi.", nameof(reason));
        if (r.Length > 500)
            r = r[..500];

        var retiredAudit = new AuditTrailType(
            AuditTrail.Created,
            AuditTrail.Modified,
            AuditTrail.Voided);
        retiredAudit.Modif(amendedBy, amendedAt);

        var retiredItems = CloneItems(Items);
        var retired = Load(
            ResultDocumentId,
            OrderId,
            VersionNo,
            isCurrentVersion: false,
            ResultSource,
            LabResultStatusEnum.Amended,
            RecordedDate,
            RecordedUserId,
            VerifiedDate,
            VerifiedUserId,
            AmendmentReason,
            AmendedDate,
            AmendedUserId,
            PreviousVersionId,
            retiredAudit,
            retiredItems,
            ExpertiseNote,
            CriticalNote);

        var newId = NunaId.New(IdPrefix);
        var newAudit = AuditTrailType.Create(amendedBy, amendedAt);
        var newItems = RenumberItems(scaffoldItems);
        var newVersion = Load(
            newId,
            OrderId,
            VersionNo + 1,
            isCurrentVersion: true,
            ResultSource,
            LabResultStatusEnum.Recorded,
            amendedAt,
            amendedBy,
            verifiedDate: EmptyDateSentinel,
            verifiedUserId: "",
            amendmentReason: r,
            amendedDate: amendedAt,
            amendedUserId: amendedBy,
            previousVersionId: ResultDocumentId,
            newAudit,
            newItems,
            ExpertiseNote,
            CriticalNote);

        return (retired, newVersion);
    }

    private static List<LabResultItemModel> CloneItems(IReadOnlyList<LabResultItemModel> items)
        => items.Select(i => new LabResultItemModel(
            i.ItemNo,
            i.ComponentId,
            i.TestId,
            i.TestName,
            i.ComponentCode,
            i.ComponentName,
            i.SequenceNo,
            i.ResultType,
            i.NumericValue,
            i.TextValue,
            i.OptionValue,
            i.NarrativeValue,
            i.Unit,
            i.ReferenceRangeText,
            i.IsMandatory,
            i.FlagStatus)).ToList();

    private static List<LabResultItemModel> RenumberItems(IReadOnlyList<LabResultItemModel> items)
    {
        var n = 1;
        return items.Select(i => new LabResultItemModel(
            n++,
            i.ComponentId,
            i.TestId,
            i.TestName,
            i.ComponentCode,
            i.ComponentName,
            i.SequenceNo,
            i.ResultType,
            i.NumericValue,
            i.TextValue,
            i.OptionValue,
            i.NarrativeValue,
            i.Unit,
            i.ReferenceRangeText,
            i.IsMandatory,
            i.FlagStatus)).ToList();
    }

    public void RecordResult(LabResultSourceEnum source, IEnumerable<LabResultItemCapture> captures, string userId, DateTime recordedAt = default)
    {
        Guard.Against.Null(captures, nameof(captures));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah void; rekaman hasil tidak diperbolehkan.");

        if (ResultStatus == LabResultStatusEnum.Verified || ResultStatus == LabResultStatusEnum.Released)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah diverifikasi; rekaman hasil tidak dapat diubah.");

        if (ResultStatus != LabResultStatusEnum.Draft && ResultStatus != LabResultStatusEnum.Recorded)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} berstatus {ResultStatus}; pembaruan hasil tidak diperbolehkan.");

        var list = captures.ToList();
        Guard.Against.NullOrEmpty(list, nameof(captures));

        ResultSource = source;

        var next = new List<LabResultItemModel>();
        var no = 1;
        foreach (var c in list)
        {
            Guard.Against.NullOrWhiteSpace(c.ComponentId, nameof(c.ComponentId));
            Guard.Against.NullOrWhiteSpace(c.TestId, nameof(c.TestId));
            Guard.Against.NullOrWhiteSpace(c.TestName, nameof(c.TestName));

            var flag = LabResultFlagging.Compute(c.ResultType, c.NumericValue, c.ReferenceRangeText ?? "");

            var narrative = c.NarrativeValue ?? "";
            if (narrative.Length > 4000)
                narrative = narrative[..4000];

            var text = c.TextValue ?? "";
            if (text.Length > 500)
                text = text[..500];

            var option = c.OptionValue ?? "";
            if (option.Length > 200)
                option = option[..200];

            next.Add(new LabResultItemModel(
                no++,
                c.ComponentId,
                c.TestId,
                c.TestName,
                c.ComponentCode ?? "",
                c.ComponentName ?? "",
                c.SequenceNo,
                c.ResultType,
                c.NumericValue,
                text,
                option,
                narrative,
                c.Unit ?? "",
                c.ReferenceRangeText ?? "",
                c.IsMandatory,
                flag));
        }

        _items.Clear();
        _items.AddRange(next);
        var recordAt = recordedAt == default ? DateTime.Now : recordedAt;
        AuditTrail.Modif(userId, recordAt);
    }

    public void MarkRecorded(string userId, DateTime recordedAt = default)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah void; MarkRecorded tidak diperbolehkan.");

        if (ResultStatus == LabResultStatusEnum.Verified || ResultStatus == LabResultStatusEnum.Released)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah diverifikasi; MarkRecorded tidak diperbolehkan.");

        var at = recordedAt == default ? DateTime.Now : recordedAt;
        ResultStatus = LabResultStatusEnum.Recorded;
        RecordedDate = at;
        RecordedUserId = userId;
        AuditTrail.Modif(userId, at);
    }

    public void SetCriticalNote(string note)
    {
        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah void; penataan catatan kritis tidak diperbolehkan.");

        if (ResultStatus == LabResultStatusEnum.Released)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah dirilis; penataan catatan kritis tidak diperbolehkan.");

        var trimmed = (note ?? string.Empty).Trim();
        if (trimmed.Length > 1000)
            trimmed = trimmed[..1000];

        CriticalNote = trimmed;
    }

    public void SetExpertiseNote(string note)
    {
        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah void; penataan catatan ekspertise tidak diperbolehkan.");

        if (ResultStatus == LabResultStatusEnum.Released)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah dirilis; penataan catatan ekspertise tidak diperbolehkan.");

        var trimmed = (note ?? string.Empty).Trim();
        if (trimmed.Length > 2000)
            trimmed = trimmed[..2000];

        ExpertiseNote = trimmed;
    }

    public void Verify(string verifiedUserId, DateTime verifiedDate, string expertiseNote = "")
    {
        Guard.Against.NullOrWhiteSpace(verifiedUserId, nameof(verifiedUserId));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah void; verifikasi tidak diperbolehkan.");

        if (ResultStatus == LabResultStatusEnum.Verified || ResultStatus == LabResultStatusEnum.Released)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah diverifikasi.");

        if (ResultStatus != LabResultStatusEnum.Recorded)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} berstatus {ResultStatus}; verifikasi hanya diperbolehkan setelah hasil direkam (Recorded).");

        var missingMandatory = _items
            .Where(i => i.IsMandatory && IsItemValueEmpty(i))
            .ToList();

        if (missingMandatory.Count > 0)
        {
            var names = string.Join(", ", missingMandatory.Select(m =>
                string.IsNullOrWhiteSpace(m.ComponentName) ? m.ComponentCode : m.ComponentName));
            throw new InvalidOperationException($"Komponen mandatori belum terisi: {names}.");
        }

        ResultStatus = LabResultStatusEnum.Verified;
        VerifiedUserId = verifiedUserId;
        VerifiedDate = verifiedDate;

        if (!string.IsNullOrWhiteSpace(expertiseNote))
        {
            var note = expertiseNote.Trim();
            if (note.Length > 2000)
                note = note[..2000];
            ExpertiseNote = note;
        }

        AuditTrail.Modif(verifiedUserId, verifiedDate);
    }

    public void MarkReleased(string userId, DateTime releasedAt = default)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah void; MarkReleased tidak diperbolehkan.");

        if (ResultStatus == LabResultStatusEnum.Released)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah dirilis.");

        if (ResultStatus != LabResultStatusEnum.Verified)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} berstatus {ResultStatus}; hanya dokumen berstatus Verified yang dapat dirilis.");

        var at = releasedAt == default ? DateTime.Now : releasedAt;
        ResultStatus = LabResultStatusEnum.Released;
        AuditTrail.Modif(userId, at);
    }

    private static bool IsItemValueEmpty(LabResultItemModel item)
    {
        return item.ResultType switch
        {
            LabResultTypeEnum.Numeric => item.NumericValue == 0 && string.IsNullOrWhiteSpace(item.TextValue),
            LabResultTypeEnum.Text => string.IsNullOrWhiteSpace(item.TextValue),
            LabResultTypeEnum.Option => string.IsNullOrWhiteSpace(item.OptionValue),
            LabResultTypeEnum.Narrative => string.IsNullOrWhiteSpace(item.NarrativeValue),
            _ => string.IsNullOrWhiteSpace(item.TextValue) && item.NumericValue == 0
        };
    }

    public string ResultDocumentId { get; init; }
    public string OrderId { get; init; }
    public int VersionNo { get; init; }
    public bool IsCurrentVersion { get; init; }
    public LabResultSourceEnum ResultSource { get; private set; }
    public LabResultStatusEnum ResultStatus { get; private set; }
    public DateTime RecordedDate { get; private set; }
    public string RecordedUserId { get; private set; }
    public DateTime VerifiedDate { get; private set; }
    public string VerifiedUserId { get; private set; }
    public string AmendmentReason { get; init; }
    public DateTime AmendedDate { get; init; }
    public string AmendedUserId { get; init; }
    public string PreviousVersionId { get; init; }
    public string ExpertiseNote { get; private set; }
    public string CriticalNote { get; private set; }
    public AuditTrailType AuditTrail { get; init; }
    public IReadOnlyList<LabResultItemModel> Items => _items;
}
