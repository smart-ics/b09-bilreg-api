namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public class BillingLockViolationException : InvalidOperationException
{
    public string RegId { get; } = string.Empty;
    public TataRekeningStatusEnum? Status { get; }

    public BillingLockViolationException(string message)
        : base(message)
    {
    }

    public BillingLockViolationException(string regId, TataRekeningStatusEnum status)
        : base($"Episode billing '{regId}' telah berstatus {status}. Penambahan, pengubahan, atau pembatalan transaksi ditolak.")
    {
        RegId = regId;
        Status = status;
    }

    public BillingLockViolationException(string regId, string message)
        : base(message)
    {
        RegId = regId;
    }

    public BillingLockViolationException(string regId, TataRekeningStatusEnum status, string message)
        : base(message)
    {
        RegId = regId;
        Status = status;
    }
}
