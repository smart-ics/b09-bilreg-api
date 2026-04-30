namespace Bilreg.Domain.AccountingContext.CoaFeature;

public record CoaTipeType : ICoaTipeKey
{
    public CoaTipeType(string coaTipeId, string coaTipeName)
    {
        CoaTipeId = coaTipeId;
        CoaTipeName = coaTipeName;
    }
    public string CoaTipeId { get; init; }
    public string CoaTipeName { get; init; }
    public static ICoaTipeKey Key(string id) => new CoaTipeType(id, "-");
    public static CoaTipeType Default => new("-", "-");
}

public interface ICoaTipeKey
{
    string CoaTipeId { get; }
}