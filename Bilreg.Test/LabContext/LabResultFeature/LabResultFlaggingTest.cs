using Bilreg.Domain.LabContext.LabResultFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultFlaggingTest
{
    [Fact]
    public void Compute_NonNumeric_AlwaysNormal()
    {
        LabResultFlagging.Compute(LabResultTypeEnum.Text, 99m, "0-10")
            .Should().Be(LabResultFlagEnum.Normal);
    }

    [Theory]
    [InlineData(4.5, "3.5-5.5", LabResultFlagEnum.Normal)]
    [InlineData(3.0, "3.5-5.5", LabResultFlagEnum.Low)]
    [InlineData(6.0, "3.5-5.5", LabResultFlagEnum.High)]
    [InlineData(4.5, "3.5 - 5.5", LabResultFlagEnum.Normal)]
    public void Compute_Range(decimal value, string range, LabResultFlagEnum expected)
    {
        LabResultFlagging.Compute(LabResultTypeEnum.Numeric, value, range).Should().Be(expected);
    }

    [Theory]
    [InlineData(4.0, "<5", LabResultFlagEnum.Normal)]
    [InlineData(5.0, "<5", LabResultFlagEnum.High)]
    [InlineData(11.0, ">10", LabResultFlagEnum.Normal)]
    [InlineData(10.0, ">10", LabResultFlagEnum.Low)]
    public void Compute_Bounds(decimal value, string range, LabResultFlagEnum expected)
    {
        LabResultFlagging.Compute(LabResultTypeEnum.Numeric, value, range).Should().Be(expected);
    }

    [Fact]
    public void Compute_UnparseableRange_ReturnsNormal()
    {
        LabResultFlagging.Compute(LabResultTypeEnum.Numeric, 100m, "see comment")
            .Should().Be(LabResultFlagEnum.Normal);
    }
}
