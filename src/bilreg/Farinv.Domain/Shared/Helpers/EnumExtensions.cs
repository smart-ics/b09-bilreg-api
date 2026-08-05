namespace Farinv.Domain.Shared.Helpers;

public static class EnumExtensions
{
    public static TEnum? ToEnumOrNull<TEnum>(this string value) where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, true, out var result))
            return result;

        return null;
    }

    public static TEnum ToEnumOrDefault<TEnum>(this string value, TEnum defaultValue) where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, true, out var result))
            return result;

        return defaultValue;
    }

    public static TEnum ToEnumStrict<TEnum>(this string value) where TEnum : struct, Enum
    {
        return Enum.Parse<TEnum>(value, true);
    }
}
