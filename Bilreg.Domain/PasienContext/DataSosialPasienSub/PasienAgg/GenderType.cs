using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public class GenderType
{
    private static readonly string[] AllowedValues = ["M", "F", "P", "W", "L", "0", "1"];

    private readonly string _value;
    
    public GenderType(string value)
    {
        if (value.Length != 1)
            throw new ArgumentException("Invalid Gender");

        _value = value.ToUpper();
        if (Array.IndexOf(AllowedValues, _value) == -1)
            throw new ArgumentException("Invalid Gender");
        
    }

    public override string ToString()
    {
        return _value;
    }
    
    public static GenderType Default => new("M");
}