using Ardalis.GuardClauses;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.PasienContext.DemografiFeature;

public record PropinsiType : IPropinsiKey
{
    public PropinsiType(string propinsiId, string propinsiName)
    {
        Guard.Against.NullOrWhiteSpace(propinsiId, nameof(propinsiId));
        Guard.Against.NullOrWhiteSpace(propinsiName, nameof(propinsiName));

        PropinsiId = propinsiId;
        PropinsiName = propinsiName;
    }
    
    public string PropinsiId { get; init; }
    public string PropinsiName { get; init; }
    
    public static PropinsiType Default => new("-", "-");
    public static IPropinsiKey Key(string id) => Default with { PropinsiId = id };
}

public interface IPropinsiKey
{
    string PropinsiId {get;}
}

public class PropinsiTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new PropinsiType("1", "2");
        sut.PropinsiId.Should().Be("1");
        sut.PropinsiName.Should().Be("2");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = PropinsiType.Default;
        sut.PropinsiId.Should().Be("-");
        sut.PropinsiName.Should().Be("-");
    }
}