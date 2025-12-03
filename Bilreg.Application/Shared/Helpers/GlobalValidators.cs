namespace Bilreg.Application.Shared.Helpers;

public static class GlobalValidator
{
    public static bool IsValidA(this string str, Func<string, bool> func)
    {
        return func(str);
    }
}