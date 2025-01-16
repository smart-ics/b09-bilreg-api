using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Xunit;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;

public record ActivityFlagVo
{
    public DateTime ActivityDate { get; } 
    public string UserId { get; }
    
    public bool IsFlagged => ActivityDate != new DateTime(3000, 1, 1);
    
    public ActivityFlagVo(DateTime activityDate, string userId)
    {
        ActivityDate = activityDate;
        UserId = userId;

        Validate();
    }
    
    private void Validate()
    {
        var isDateSet = ActivityDate != new DateTime(3000, 1, 1);
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
        var actual = new ActivityFlagVo(new DateTime(2024,10,6), "A");
        actual.IsFlagged.Should().BeTrue();
    }
    
    [Fact]
    public void T02_GivenVoidDateEmpty_ThenIsVoidFalse()
    {
        var actual = new ActivityFlagVo(new DateTime(3000, 1, 1), "");
        actual.IsFlagged.Should().BeFalse();
    }
    
    [Fact]
    public void T03_GivenVoidDateIsSet_ButUserIdIsEmpty_ThenThrowEx()
    {
        var actual = () => new ActivityFlagVo(new DateTime(2024,10,6), "");
        actual.Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void T04_GivenVoidDateIsEmpty_ButUserIdIsSet_ThenThrowEx()
    {
        var actual = () => new ActivityFlagVo(new DateTime(3000, 1, 1), "A");
        actual.Should().Throw<ArgumentException>();
    }
}