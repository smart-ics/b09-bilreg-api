using Ardalis.GuardClauses;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.PasienContext.StatusSosialFeature;

public record PekerjaanDkType : IPekerjaanDkKey
{
    public PekerjaanDkType(string pekerjaanDkId, string pekerjaanDkName)
    {
        Guard.Against.NullOrWhiteSpace(pekerjaanDkId, nameof(pekerjaanDkId));
        Guard.Against.NullOrWhiteSpace(pekerjaanDkName, nameof(pekerjaanDkName));

        PekerjaanDkId = pekerjaanDkId;
        PekerjaanDkName = pekerjaanDkName;
    }
    
    public string PekerjaanDkId { get; init; }
    public string PekerjaanDkName { get; init; }
    
    public static PekerjaanDkType Default => new("-", "-");
    public static IPekerjaanDkKey Key(string id) => Default with { PekerjaanDkId = id };
}

public interface IPekerjaanDkKey
{
    string PekerjaanDkId {get;}
}

public class PekerjaanDkTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new PekerjaanDkType("1", "2");
        sut.PekerjaanDkId.Should().Be("1");
        sut.PekerjaanDkName.Should().Be("2");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = PekerjaanDkType.Default;
        sut.PekerjaanDkId.Should().Be("-");
        sut.PekerjaanDkName.Should().Be("-");
    }
}