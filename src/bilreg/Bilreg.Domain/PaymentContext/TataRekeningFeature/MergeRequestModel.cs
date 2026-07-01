namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

/// <summary>
/// Merge Request — intent to consolidate Billing Sets across registrations (SOP-TR-03).
/// Not part of the Tata Rekening aggregate; input to Merge Billing domain service.
/// </summary>
public sealed class MergeRequestModel
{
    private MergeRequestModel(
        string mergeRequestId,
        string sourceRegId,
        string? targetRegId,
        MergeRequestStatusEnum status)
    {
        MergeRequestId = mergeRequestId;
        SourceRegId = sourceRegId;
        TargetRegId = targetRegId;
        Status = status;
    }

    public string MergeRequestId { get; }
    public string SourceRegId { get; }
    public string? TargetRegId { get; private set; }
    public MergeRequestStatusEnum Status { get; private set; }

    public static MergeRequestModel Create(string mergeRequestId, string sourceRegId, string? targetRegId = null)
    {
        if (string.IsNullOrWhiteSpace(mergeRequestId))
            throw new ArgumentException("Merge Request id tidak boleh kosong.", nameof(mergeRequestId));

        if (string.IsNullOrWhiteSpace(sourceRegId))
            throw new ArgumentException("Source Registrasi tidak boleh kosong.", nameof(sourceRegId));

        return new MergeRequestModel(mergeRequestId, sourceRegId, targetRegId, MergeRequestStatusEnum.Pending);
    }

    public void Execute()
    {
        if (Status != MergeRequestStatusEnum.Pending)
            throw new InvalidOperationException(
                $"Merge Request hanya dapat dieksekusi saat berstatus Pending (status saat ini: {Status}).");

        if (string.IsNullOrWhiteSpace(TargetRegId))
            throw new InvalidOperationException(
                "Merge Request memerlukan Target Registrasi sebelum dieksekusi.");

        Status = MergeRequestStatusEnum.Executed;
    }

    public void Cancel()
    {
        if (Status != MergeRequestStatusEnum.Pending)
            throw new InvalidOperationException(
                $"Merge Request hanya dapat dibatalkan saat berstatus Pending (status saat ini: {Status}).");

        Status = MergeRequestStatusEnum.Cancelled;
    }

    public void AssignTarget(string targetRegId)
    {
        if (Status != MergeRequestStatusEnum.Pending)
            throw new InvalidOperationException(
                "Target Registrasi hanya dapat ditetapkan pada Merge Request berstatus Pending.");

        if (string.IsNullOrWhiteSpace(targetRegId))
            throw new ArgumentException("Target Registrasi tidak boleh kosong.", nameof(targetRegId));

        TargetRegId = targetRegId;
    }
}
