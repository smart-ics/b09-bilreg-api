using Ardalis.GuardClauses;
using Nuna.Lib.ValidationHelper;

namespace Farinv.Domain.Shared.Helpers;

public static class GuardExtensions
{
    public static T NotInAllowedValues<T>(this IGuardClause guardClause,
        T input,
        IEnumerable<T> allowedValues,
        string parameterName,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(allowedValues);
        var enumerable = allowedValues as T[] ?? allowedValues.ToArray();
        if (enumerable.Contains(input)) return input;
        
        var allowedValuesString = string.Join(", ", enumerable);
        var errorMessage = message ?? 
            $"Parameter '{parameterName}' must be one of: {allowedValuesString}. Actual value: {input}";
                
        throw new ArgumentException(errorMessage, parameterName);
    }

    public static string InvalidDateFormat(this IGuardClause guardClause, string input, string parameterName)
    {
        if (input.IsValidTgl("yyyy-MM-dd")) return input;
        const string ERROR_MESSAGE = "Invalid date format";
        throw new ArgumentException(ERROR_MESSAGE, parameterName);
    }

    public static string InvalidTimeFormat(this IGuardClause guardClause, string input, string parameterName)
    {
        if (input.IsValidJam("HH:mm")) return input;
        const string ERROR_MESSAGE = "Invalid time format";
        throw new ArgumentException(ERROR_MESSAGE, parameterName);
    }
}