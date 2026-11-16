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
        : base(warnings != null ? string.Join("; ", warnings) : string.Empty)
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
}
