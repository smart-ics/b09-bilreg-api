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

public enum TrsBillRegStatusEnum
{
    Open,
    Close,
    Final,
}