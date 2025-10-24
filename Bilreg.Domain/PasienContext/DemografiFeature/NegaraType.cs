using Ardalis.GuardClauses;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.PasienContext.DemografiFeature;

public record NegaraType : INegaraKey
{
    public NegaraType(string negaraId, string negaraName, string negaraEngName, 
        string alpha3, string negaraNumeric)
    {
        Guard.Against.NullOrWhiteSpace(negaraId, nameof(negaraId));
        Guard.Against.NullOrWhiteSpace(negaraName, nameof(negaraName));

        NegaraId = negaraId;
        NegaraName = negaraName;
        NegaraEngName = negaraEngName;
        Alpha3 = alpha3;
        NegaraNumeric = negaraNumeric;
    }
    public string NegaraId { get; init; }
    public string NegaraName { get; init; }
    public string NegaraEngName { get; init; }
    public string Alpha3 { get; init; }
    public string NegaraNumeric { get; init; }

    public static NegaraType Default => new NegaraType("-", "-", "-", "-", "-");
    public static INegaraKey Key(string id) => Default with { NegaraId = id };
}

public interface INegaraKey
{
    string NegaraId { get;}
}

public class NegaraTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new NegaraType("1", "2", "3", "4", "5");
        sut.NegaraId.Should().Be("1");
        sut.NegaraName.Should().Be("2");
        sut.NegaraEngName.Should().Be("3");
        sut.Alpha3.Should().Be("4");
        sut.NegaraNumeric.Should().Be("5");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = NegaraType.Default;
        sut.NegaraId.Should().Be("-");
        sut.NegaraName.Should().Be("-");
        sut.NegaraEngName.Should().Be("-");
        sut.Alpha3.Should().Be("-");
        sut.NegaraNumeric.Should().Be("-");
    }
}