using Xunit;

namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public class GolDarahType
{
    private static readonly string[] AllowedValues = ["A", "B", "AB", "O"];
    private readonly string _value;
        
    public GolDarahType(string value)
    {
        _value = value.ToUpper();
        if (Array.IndexOf(AllowedValues, _value) == -1)
            throw new ArgumentException("Invalid GolDarah");
    }

    public override string ToString()
    {
        return _value;
    }
    
    public static GolDarahType Default => new("O");
}

public class GolDarahTypeTest
{
    [Fact]
    public void GivenA_WhenToString_ThenShouldReturnA()
    {
        var sut = new GolDarahType("A");
        Assert.Equal("A", sut.ToString());        
    }
}