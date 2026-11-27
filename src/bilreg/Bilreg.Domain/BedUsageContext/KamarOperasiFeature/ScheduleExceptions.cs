namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public class ScheduleCollisionException : Exception
{
    public ScheduleCollisionException()
    {
    }

    public ScheduleCollisionException(string message) : base(message)
    {
    }

    public ScheduleCollisionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class ScheduleWarningException : Exception
{
    public IEnumerable<string> Warnings { get; }

    public ScheduleWarningException(IEnumerable<string> warnings)
        : base(FormatMessage(warnings))
    {
        Warnings = warnings?.ToList() ?? new List<string>();
    }

    public ScheduleWarningException(string message, IEnumerable<string> warnings)
        : base(message)
    {
        Warnings = warnings?.ToList() ?? new List<string>();
    }

    public ScheduleWarningException(string message)
        : base(message)
    {
        Warnings = new List<string> { message };
    }

    private static string FormatMessage(IEnumerable<string>? warnings)
    {
        var list = warnings?.ToList();
        if (list == null || list.Count == 0)
            return string.Empty;
        if (list.Count == 1)
            return list[0];
        return $"Terdapat {list.Count} peringatan pada jadwal operasi.";
    }
}
