using Ardalis.GuardClauses;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.PasienContext.StatusSosialFeature;

public record AgamaType : IAgamaKey
{
    public AgamaType(string agamaId, string agamaName)
    {
        Guard.Against.NullOrWhiteSpace(agamaId, nameof(agamaId));
        Guard.Against.NullOrWhiteSpace(agamaName, nameof(agamaName));

        AgamaId = agamaId;
        AgamaName = agamaName;
    }
    
    public string AgamaId { get; init; }
    public string AgamaName { get; init; }
    
    public static AgamaType Default => new("-", "-");
    public static IAgamaKey Key(string id) => Default with { AgamaId = id };
}

public interface IAgamaKey
{
    string AgamaId {get;}
}

public class AgamaTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new AgamaType("1", "2");
        sut.AgamaId.Should().Be("1");
        sut.AgamaName.Should().Be("2");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = AgamaType.Default;
        sut.AgamaId.Should().Be("-");
        sut.AgamaName.Should().Be("-");
    }
}