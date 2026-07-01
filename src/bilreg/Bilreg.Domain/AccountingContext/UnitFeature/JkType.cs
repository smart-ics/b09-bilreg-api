namespace Bilreg.Domain.AccountingContext.UnitFeature;

public record JkType : IJkKey
{
    public JkType(string jkId, string jkName, int noUrut)
    {
        JkId = jkId;
        JkName = jkName;
        NoUrut = noUrut;
    }
    public string JkId { get; init; }
    public string JkName { get; init; }
    public int NoUrut { get; init; }

    public static JkType Default => new("-", "-", 0);
}

public interface IJkKey
{
    string JkId {get;}
}
public record JkReff(string JkId, string JkName);
