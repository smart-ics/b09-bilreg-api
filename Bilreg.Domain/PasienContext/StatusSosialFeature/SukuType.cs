using Ardalis.GuardClauses;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.PasienContext.StatusSosialFeature;

public record SukuType : ISukuKey
{
    public SukuType(string sukuId, string sukuName)
    {
        Guard.Against.NullOrWhiteSpace(sukuId, nameof(sukuId));
        Guard.Against.NullOrWhiteSpace(sukuName, nameof(sukuName));

        SukuId = sukuId;
        SukuName = sukuName;
    }
    
    public string SukuId { get; init; }
    public string SukuName { get; init; }
    
    public static SukuType Default => new("-", "-");
    public static ISukuKey Key(string id) => Default with { SukuId = id };
}

public interface ISukuKey
{
    string SukuId {get;}
}

public class SukuTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new SukuType("1", "2");
        sut.SukuId.Should().Be("1");
        sut.SukuName.Should().Be("2");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = SukuType.Default;
        sut.SukuId.Should().Be("-");
        sut.SukuName.Should().Be("-");
    }
}
