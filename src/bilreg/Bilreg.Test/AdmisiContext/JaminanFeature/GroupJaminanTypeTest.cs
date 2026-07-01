using Bilreg.Domain.AdmisiContext.JaminanFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.JaminanFeature;

public class GroupJaminanTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new GroupJaminanType("1", "2", false, "");
        sut.GroupJaminanId.Should().Be("1");
        sut.GroupJaminanName.Should().Be("2");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = GroupJaminanType.Default;
        sut.GroupJaminanId.Should().Be("-");
        sut.GroupJaminanName.Should().Be("-");
    }
}