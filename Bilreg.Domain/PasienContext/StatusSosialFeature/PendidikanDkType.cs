using Ardalis.GuardClauses;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.PasienContext.StatusSosialFeature;

public record PendidikanDkType : IPendidikanDkKey
{
    public PendidikanDkType(string pendidikanDkId, string pendidikanDkName)
    {
        Guard.Against.NullOrWhiteSpace(pendidikanDkId, nameof(pendidikanDkId));
        Guard.Against.NullOrWhiteSpace(pendidikanDkName, nameof(pendidikanDkName));

        PendidikanDkId = pendidikanDkId;
        PendidikanDkName = pendidikanDkName;
    }
    
    public string PendidikanDkId { get; init; }
    public string PendidikanDkName { get; init; }
    
    public static PendidikanDkType Default => new("-", "-");
    public static IPendidikanDkKey Key(string id) => Default with { PendidikanDkId = id };
}

public interface IPendidikanDkKey
{
    string PendidikanDkId {get;}
}

public class PendidikanDkTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new PendidikanDkType("1", "2");
        sut.PendidikanDkId.Should().Be("1");
        sut.PendidikanDkName.Should().Be("2");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = PendidikanDkType.Default;
        sut.PendidikanDkId.Should().Be("-");
        sut.PendidikanDkName.Should().Be("-");
    }
}