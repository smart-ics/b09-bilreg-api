namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public record TrsBillRegType
{
    public string RegId { get; init; }
    public TrsBillRegStatusEnum Status { get; init; }
}

// public record TrsBillKodeBayarType
// {
//     public string 
// }

public record TrsBillPaymentType (string PaymentId, string PaymentName, bool IsTipeJaminan)
{
    public static TrsBillPaymentType ByKas => new("BYKAS", "KAS", false);
    public static TrsBillPaymentType ByPri => new("BYPRI", "Hutang Pribadi", false);
    public static TrsBillPaymentType ByDpu => new("BYDPU", "Deposit", false);
    public static TrsBillPaymentType ByVch => new("BYVCH", "Voucher", false);
    public static TrsBillPaymentType ByDpk => new("BYDPK", "Deposit Khusus", false);
}
public enum TrsBillRegStatusEnum
{
    Open,
    Close,
    Final,
}