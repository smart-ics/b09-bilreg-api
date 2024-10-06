using CommunityToolkit.Diagnostics;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;

public record TglJamTrsVo
{
    public DateTime TglJam {get;} 
    public string UserId {get;}

    public TglJamTrsVo(DateTime date, string userId)
    {
        TglJam = date;
        UserId = userId;

        ValidateArg();
    }

    private void ValidateArg()
    {
        Guard.IsTrue(TglJam != new DateTime(3000,1,1));
        Guard.IsNotNullOrEmpty(UserId);
    }
};

public class TglJamTrsVoTest
{
    [Fact]
    public void T01_GivenValidArg_ThenSuccess()
    {
        var actual = new TglJamTrsVo(new DateTime(2024, 10, 6), "A");
    }

    [Fact]
    public void T02_GivenEmptyTrsDate_ThenThrowEx()
    {
        var actual = () => new TglJamTrsVo(new DateTime(3000, 1, 1), "A");
        actual.Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void T03_GivenEmptyUserId_ThenThrowEx()
    {
        var actual = () => new TglJamTrsVo(new DateTime(2024, 10, 6), "");
        actual.Should().Throw<ArgumentException>();
    }
    
}