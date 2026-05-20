using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.LabContext.LabOrderFeature;

public class BillingReleaseCheckModel
{
    private const string IdPrefix = "LBC";

    private BillingReleaseCheckModel(
        string checkId,
        string orderId,
        BillingReleaseStatusEnum billingStatus,
        string message,
        string requestedByUserId,
        DateTime checkedAt)
    {
        CheckId = checkId;
        OrderId = orderId;
        BillingStatus = billingStatus;
        Message = message;
        RequestedByUserId = requestedByUserId;
        CheckedAt = checkedAt;
    }

    public static BillingReleaseCheckModel Create(
        string orderId,
        BillingReleaseStatusEnum billingStatus,
        string message,
        string requestedByUserId,
        DateTime checkedAt)
    {
        Guard.Against.NullOrWhiteSpace(orderId, nameof(orderId));
        Guard.Against.NullOrWhiteSpace(requestedByUserId, nameof(requestedByUserId));
        Guard.Against.Null(message, nameof(message));

        var msg = message.Length > 200 ? message[..200] : message;
        return new BillingReleaseCheckModel(
            NunaId.New(IdPrefix),
            orderId,
            billingStatus,
            msg,
            requestedByUserId,
            checkedAt);
    }

    public static BillingReleaseCheckModel Load(
        string checkId,
        string orderId,
        BillingReleaseStatusEnum billingStatus,
        string message,
        string requestedByUserId,
        DateTime checkedAt)
        => new(checkId, orderId, billingStatus, message, requestedByUserId, checkedAt);

    public string CheckId { get; }
    public string OrderId { get; }
    public BillingReleaseStatusEnum BillingStatus { get; }
    public string Message { get; }
    public string RequestedByUserId { get; }
    public DateTime CheckedAt { get; }
}
