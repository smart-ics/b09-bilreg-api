using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers;

namespace Bilreg.Domain.PasienContext.PasienFeature;

public class GolDarahType
{
    private static readonly string[] AllowedValues = ["A", "B", "AB", "O", "-"];
    private readonly string _value;
        
    public GolDarahType(string value)
    {
        value = value.Trim();
        if (value == "")
            value = "-";

        if (value == "0")
            value = "O";
        
        Guard.Against.NotInAllowedValues(value.ToUpper(), AllowedValues, nameof(value));
        _value = value.ToUpper();
    }

    public override string ToString()
    {
        return _value;
    }
    
    public static GolDarahType Default => new("O");
    public static GolDarahType A => new("A");
    public static GolDarahType B => new("B");
    public static GolDarahType AB => new("AB");
    public static GolDarahType O => new("O");
}
