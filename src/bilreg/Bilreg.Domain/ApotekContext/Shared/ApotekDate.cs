namespace Bilreg.Domain.ApotekContext.Shared;

public static class ApotekDate
{
    public static readonly DateTime Empty = new(3000, 1, 1);

    public static bool IsEmpty(DateTime value) => value.Date >= Empty.Date;
}
