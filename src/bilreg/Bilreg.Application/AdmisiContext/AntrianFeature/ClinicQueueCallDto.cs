namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public sealed class ClinicQueueCallDto
{
    public string PoliId { get; set; } = string.Empty;
    public string DokterId { get; set; } = string.Empty;
    public string RuangId { get; set; } = string.Empty;
    public string AntrianId { get; set; } = string.Empty;
    public int NoUrut { get; set; }
    public DateTime CalledAt { get; set; }
    public int CallCount { get; set; }
    public int CallStatus { get; set; } // 0: Active, 1: Skipped, 2: Dismissed
    public string UserId { get; set; } = string.Empty;
    public DateTime CrtDate { get; set; }
    public DateTime UpdDate { get; set; }
}

public static class ClinicQueueCallStatus
{
    public const int Active = 0;
    public const int Skipped = 1;
    public const int Dismissed = 2;
}

public sealed class AdmissionDisplayPoliDto
{
    public string DisplayId { get; set; } = string.Empty;
    public string PoliId { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
