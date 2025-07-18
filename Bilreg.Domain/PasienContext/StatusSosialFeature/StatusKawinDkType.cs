using Ardalis.GuardClauses;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.PasienContext.StatusSosialFeature;

public record StatusKawinDkType : IStatusKawinDkKey
{
    public StatusKawinDkType(string statusKawinDkId, string statusKawinDkName)
    {
        Guard.Against.NullOrWhiteSpace(statusKawinDkId, nameof(statusKawinDkId));
        Guard.Against.NullOrWhiteSpace(statusKawinDkName, nameof(statusKawinDkName));

        StatusKawinDkId = statusKawinDkId;
        StatusKawinDkName = statusKawinDkName;
    }
    
    public string StatusKawinDkId { get; init; }
    public string StatusKawinDkName { get; init; }
    
    public static StatusKawinDkType Default => new("-", "-");
    public static IStatusKawinDkKey Key(string id) => Default with { StatusKawinDkId = id };
}

public interface IStatusKawinDkKey
{
    string StatusKawinDkId {get;}
}

public class StatusKawinDkTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new StatusKawinDkType("1", "2");
        sut.StatusKawinDkId.Should().Be("1");
        sut.StatusKawinDkName.Should().Be("2");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = StatusKawinDkType.Default;
        sut.StatusKawinDkId.Should().Be("-");
        sut.StatusKawinDkName.Should().Be("-");
    }
}