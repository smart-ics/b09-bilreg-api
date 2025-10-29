using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.AdmisiContext.RegFeature.RegAgg.ValueObjects;

public record VoidFlagType
{
    public DateTime VoidDate { get; } 
    public string UserId { get; }
    public string TglVoid => VoidDate.ToString("yyyy-MM-dd");
    public string JamVoid => VoidDate.ToString("HH:mm:ss");
    
    public bool IsVoid => VoidDate != new DateTime(3000, 1, 1);
    
    public VoidFlagType(DateTime voidDate, string userId)
    {
        VoidDate = voidDate;
        UserId = userId;

        Validate();
    }
    
    private void Validate()
    {
        var isDateSet = VoidDate != new DateTime(3000, 1, 1);
        var isUserIdSet = UserId.Length != 0;
        if (isDateSet ^ isUserIdSet)
            throw new ArgumentException("VoidDate-UserId invalid");
    }
}

public class ActivityFlagVoTest
{
    [Fact]
    public void T01_GivenVoidDateIsSet_ThenIsVoidTrue()
    {
        var actual = new VoidFlagType(new DateTime(2024,10,6), "A");
        actual.IsVoid.Should().BeTrue();
    }
    
    [Fact]
    public void T02_GivenVoidDateEmpty_ThenIsVoidFalse()
    {
        var actual = new VoidFlagType(new DateTime(3000, 1, 1), "");
        actual.IsVoid.Should().BeFalse();
    }
    
    [Fact]
    public void T03_GivenVoidDateIsSet_ButUserIdIsEmpty_ThenThrowEx()
    {
        var actual = () => new VoidFlagType(new DateTime(2024,10,6), "");
        actual.Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void T04_GivenVoidDateIsEmpty_ButUserIdIsSet_ThenThrowEx()
    {
        var actual = () => new VoidFlagType(new DateTime(3000, 1, 1), "A");
        actual.Should().Throw<ArgumentException>();
    }
}