using Ardalis.GuardClauses;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.PasienContext;

public record GenderType : IGenderKey
{
    public GenderType(string symbol, string description, SexDkEnum sex)
    {
        Guard.Against.NullOrWhiteSpace(symbol, nameof(symbol));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.OutOfRange((int)sex, nameof(sex), 0, 1);
        
        Symbol = symbol;
        Description = description;
        Sex = sex;
    }
    
    public string Symbol { get; init; }
    public string Description { get; init; }
    public SexDkEnum Sex { get; init; }
    public GenderReff ToReff => new(Symbol, Description);
    
    
    public static IGenderKey Key(string id) => new GenderType(id, "-", SexDkEnum.Female);
    public static GenderType Default => new("-", "-", SexDkEnum.Female);
    
}

public record GenderReff(string Symbol, string Description); 

public enum SexDkEnum
{
    Female = 0,
    Male = 1,
}

public interface IGenderKey
{
    string Symbol {get;}
}

public class GenderTypeTest
{
    [Theory]
    [InlineData("M", "Male", SexDkEnum.Male, true)]
    [InlineData("F", "Female", SexDkEnum.Female, true)]
    [InlineData("", "Unknown", SexDkEnum.Male, false)]
    [InlineData("M", "", SexDkEnum.Male, false)]
    [InlineData("", "", SexDkEnum.Male, false)]
    [InlineData("M", "Female", (SexDkEnum)2, false)]
    public void UT1_CreationTest(string symbol, string description, SexDkEnum sex, bool isOk)
    {
        var actual = () => new GenderType(symbol, description, sex);
        if (isOk)
            actual.Should().NotThrow();
        else
            actual.Should().Throw<Exception>();
    }
}