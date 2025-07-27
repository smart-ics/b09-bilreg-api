using Ardalis.GuardClauses;

namespace Bilreg.Domain.Helpers;

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
}