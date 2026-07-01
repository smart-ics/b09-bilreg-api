namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public record PaymentType (string PaymentId, string PaymentName, bool IsTipeJaminan)
{
    public static PaymentType ByKas => new("BYKAS", "KAS", false);
    public static PaymentType ByPri => new("BYPRI", "Hutang Pribadi", false);
    public static PaymentType ByDpu => new("BYDPU", "Deposit", false);
    public static PaymentType ByVch => new("BYVCH", "Voucher", false);
    public static PaymentType ByDpk => new("BYDPK", "Deposit Khusus", false);
}