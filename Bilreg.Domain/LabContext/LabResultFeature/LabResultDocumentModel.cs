using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.LabContext.LabResultFeature;

public class LabResultDocumentModel : ILabResultDocumentKey
{
    private const string IdPrefix = "LRD";
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
        AuditTrailType auditTrail,
        IEnumerable<LabResultItemModel> items)
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
        AuditTrail = auditTrail;
        _items = items.ToList();
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
        AuditTrailType.Default,
        []);

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
        AuditTrailType auditTrail,
        IEnumerable<LabResultItemModel> items)
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
            auditTrail,
            items);

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
            auditTrail,
            []);
    }

    public void RecordResult(LabResultSourceEnum source, IEnumerable<LabResultItemCapture> captures, string userId)
    {
        Guard.Against.Null(captures, nameof(captures));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah void; rekaman hasil tidak diperbolehkan.");

        if (ResultStatus == LabResultStatusEnum.Verified)
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
                c.TestId,
                c.TestName,
                c.ComponentCode ?? "",
                c.ComponentName ?? "",
                c.ResultType,
                c.NumericValue,
                text,
                option,
                narrative,
                c.Unit ?? "",
                c.ReferenceRangeText ?? "",
                flag));
        }

        _items.Clear();
        _items.AddRange(next);
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void MarkRecorded(string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah void; MarkRecorded tidak diperbolehkan.");

        if (ResultStatus == LabResultStatusEnum.Verified)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah diverifikasi; MarkRecorded tidak diperbolehkan.");

        ResultStatus = LabResultStatusEnum.Recorded;
        RecordedDate = DateTime.Now;
        RecordedUserId = userId;
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void Verify(string verifiedUserId, DateTime verifiedDate)
    {
        Guard.Against.NullOrWhiteSpace(verifiedUserId, nameof(verifiedUserId));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah void; verifikasi tidak diperbolehkan.");

        if (ResultStatus == LabResultStatusEnum.Verified)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} sudah diverifikasi.");

        if (ResultStatus != LabResultStatusEnum.Recorded)
            throw new InvalidOperationException(
                $"LabResultDocument {ResultDocumentId} berstatus {ResultStatus}; verifikasi hanya diperbolehkan setelah hasil direkam (Recorded).");

        ResultStatus = LabResultStatusEnum.Verified;
        VerifiedUserId = verifiedUserId;
        VerifiedDate = verifiedDate;
        AuditTrail.Modif(verifiedUserId, DateTime.Now);
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
    public AuditTrailType AuditTrail { get; init; }
    public IReadOnlyList<LabResultItemModel> Items => _items;
}
