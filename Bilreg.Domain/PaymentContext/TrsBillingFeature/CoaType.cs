namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public record CoaType : ICoaKey
{
    #region CREATION
    public CoaType(string coaId, string coaName)
    {
        CoaId = coaId;
        CoaName = coaName;
    }
    public static CoaType Default => new("-", "-");
    public static ICoaKey Key(string id) => Default with { CoaId = id };
    #endregion

    #region PROPERTIES
    public string CoaId { get; init; }
    public string CoaName { get; init; }
    #endregion
}

public interface ICoaKey
{
    string CoaId { get; }
}
