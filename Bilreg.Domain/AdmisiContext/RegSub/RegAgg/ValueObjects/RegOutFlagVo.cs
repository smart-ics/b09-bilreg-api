using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;

public record RegOutFlagVo
{
    public DateTime RegOutDate { get; }
    public string UserId { get; }
    public bool IsOut => RegOutDate != new DateTime(3000, 1, 1);

    public RegOutFlagVo(DateTime regOutDate, string userId)
    {
        RegOutDate = regOutDate;
        UserId = userId;

        Validate();
    }

    private void Validate()
    {
        var isDateSet = RegOutDate != new DateTime(3000, 1, 1);
        var isUserIdSet = UserId.Length != 0;
        if (isDateSet ^ isUserIdSet)
            throw new ArgumentException("RegOutDate-UserId invalid");
    }
}

public class RegOutStampVoTests
{
    [Fact]
    public void T01_GivenRegOutDateIsSet_ThenIsOutIsTrue()
    {
        var actual = new RegOutFlagVo(new DateTime(2024, 10, 6), "A");
        actual.IsOut.Should().BeTrue();
    }

    [Fact]
    public void T02_GivenEmptyRegOutDate_ThenThrowEx()
    {
        var actual = () => new RegOutFlagVo(new DateTime(3000, 1, 1), "A");
        actual.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void T03_GivenRegOutDateIsSet_ButUserIdIsEmpty_ThenThrowEx()
    {
        var actual = () => new RegOutFlagVo(new DateTime(2024,10,6), "");
        actual.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void T03_GivenRegOutDateIsEmpty_ButUserIdIsSet_ThenThrowEx()
    {
        var actual = () => new RegOutFlagVo(new DateTime(3000,1,1), "A");
        actual.Should().Throw<ArgumentException>();
    }
}