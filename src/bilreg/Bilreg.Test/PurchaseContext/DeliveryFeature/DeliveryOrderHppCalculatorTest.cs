using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using FluentAssertions;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryOrderHppCalculatorTest
{
    [Theory]
    [InlineData(HppMethodEnum.Hpp, 100, 10, 11, 100)]
    [InlineData(HppMethodEnum.HppDiskon, 100, 10, 11, 90)]
    [InlineData(HppMethodEnum.HppDiskonTax, 100, 10, 11, 101)]
    public void Calculate_SupportedMethod_ReturnsExpectedValue(
        HppMethodEnum method,
        decimal hargaBeli,
        decimal diskon,
        decimal tax,
        decimal expected)
    {
        var result = DeliveryOrderHppCalculator.Calculate(method, hargaBeli, diskon, tax);

        result.Should().Be(expected);
    }

    [Fact]
    public void Calculate_ZeroInputs_ReturnsZero()
    {
        DeliveryOrderHppCalculator.Calculate(HppMethodEnum.HppDiskonTax, 0, 0, 0)
            .Should().Be(0);
    }

    [Fact]
    public void Calculate_DecimalInputs_PreservesPrecision()
    {
        DeliveryOrderHppCalculator.Calculate(HppMethodEnum.HppDiskonTax, 10.125m, 0.025m, 1.005m)
            .Should().Be(11.105m);
    }

    [Fact]
    public void Calculate_DiscountExceedsPrice_AllowsNegativeResult()
    {
        DeliveryOrderHppCalculator.Calculate(HppMethodEnum.HppDiskon, 10, 15, 0)
            .Should().Be(-5);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void Calculate_UnknownMethod_Throws(int method)
    {
        var act = () => DeliveryOrderHppCalculator.Calculate((HppMethodEnum)method, 100, 10, 11);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("method");
    }

    [Theory]
    [InlineData(-1, 0, 0, "hargaBeli")]
    [InlineData(0, -1, 0, "diskon")]
    [InlineData(0, 0, -1, "tax")]
    public void Calculate_NegativeInput_Throws(
        decimal hargaBeli,
        decimal diskon,
        decimal tax,
        string parameterName)
    {
        var act = () => DeliveryOrderHppCalculator.Calculate(
            HppMethodEnum.HppDiskonTax, hargaBeli, diskon, tax);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(parameterName);
    }
}
