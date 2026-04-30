namespace Bilreg.Domain.AccountingContext.CoaFeature;

public record CoaType : ICoaKey
{
    public CoaType(string coaId, string coaName, CoaTipeType coaTipeType)
    {
        CoaId = coaId;
        CoaName = coaName;
        CoaTipeType = coaTipeType;
    }

    public static CoaType Default => new("-", "-", CoaTipeType.Default);
    public static ICoaKey Key(string id) => Default with { CoaId = id };

    #region PROPERTIES
    public string CoaId { get; init; }
    public string CoaName { get; init; }
    public CoaTipeType CoaTipeType { get; init; }
    #endregion
}

public interface ICoaKey
{
    string CoaId { get; }
}