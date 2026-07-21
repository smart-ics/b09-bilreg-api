using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;

namespace Bilreg.Test.Shared.BusinessDateFeature;

public class BusinessDateOptionsValidatorTest
{
    [Fact]
    public void FixedModeWithDate_SucceedsValidationRegardlessOfEnvironment()
    {
        var result = new BusinessDateOptionsValidator().Validate(null, new BusinessDateOptions
        {
            Mode = BusinessDateMode.Fixed,
            FixedDate = new DateOnly(2025, 5, 3)
        });

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void FixedModeWithoutDate_FailsValidation()
    {
        var result = new BusinessDateOptionsValidator().Validate(null, new BusinessDateOptions
        {
            Mode = BusinessDateMode.Fixed
        });

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void SystemModeWithFixedDate_FailsValidation()
    {
        var result = new BusinessDateOptionsValidator().Validate(null, new BusinessDateOptions
        {
            Mode = BusinessDateMode.System,
            FixedDate = new DateOnly(2025, 5, 3)
        });

        result.Failed.Should().BeTrue();
    }
}
