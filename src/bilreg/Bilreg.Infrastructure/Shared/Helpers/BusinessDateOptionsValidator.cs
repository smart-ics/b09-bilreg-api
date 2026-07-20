using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.Shared.Helpers;

public class BusinessDateOptionsValidator : IValidateOptions<BusinessDateOptions>
{
    private readonly bool _isProduction;

    public BusinessDateOptionsValidator(bool isProduction)
    {
        _isProduction = isProduction;
    }

    public ValidateOptionsResult Validate(string? name, BusinessDateOptions options)
    {
        if (options.Mode == BusinessDateMode.Fixed && _isProduction)
        {
            return ValidateOptionsResult.Fail(
                "Business Date Mode=Fixed is not allowed in Production. " +
                "Use Mode=System so the application uses SQL Server time.");
        }

        if (options.Mode == BusinessDateMode.Fixed && options.FixedDate is null)
        {
            return ValidateOptionsResult.Fail(
                "Business Date Mode=Fixed requires FixedDate to be configured " +
                "(example: \"FixedDate\": \"2025-05-03\").");
        }

        if (options.Mode == BusinessDateMode.System && options.FixedDate is not null)
        {
            return ValidateOptionsResult.Fail(
                "Business Date Mode=System must not set FixedDate. " +
                "Remove FixedDate or set Mode=Fixed.");
        }

        return ValidateOptionsResult.Success;
    }
}
