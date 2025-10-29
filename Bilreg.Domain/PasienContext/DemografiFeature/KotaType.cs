using Ardalis.GuardClauses;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.PasienContext.DemografiFeature;

public record KotaType : IKotaKey
{
    public KotaType(string kotaId, string kotaName)
    {
        Guard.Against.NullOrWhiteSpace(kotaId, nameof(kotaId));
        Guard.Against.NullOrWhiteSpace(kotaName, nameof(kotaName));

        KotaId = kotaId;
        KotaName = kotaName;
    }
    
    public string KotaId { get; init; }
    public string KotaName { get; init; }
    
    public static KotaType Default => new("-", "-");
    public static IKotaKey Key(string id) => Default with { KotaId = id };
}

public interface IKotaKey
{
    string KotaId {get;}
}

public class KotaTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new KotaType("1", "2");
        sut.KotaId.Should().Be("1");
        sut.KotaName.Should().Be("2");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = KotaType.Default;
        sut.KotaId.Should().Be("-");
        sut.KotaName.Should().Be("-");
    }
}