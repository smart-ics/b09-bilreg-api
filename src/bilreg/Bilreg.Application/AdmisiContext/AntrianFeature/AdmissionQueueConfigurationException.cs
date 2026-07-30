namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public sealed class AdmissionQueueConfigurationException : InvalidOperationException
{
    public AdmissionQueueConfigurationException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}

public static class AdmissionQueueConfigurationErrorCodes
{
    public const string Invalid = "AQ_CONFIG_INVALID";
    public const string WorkstationNotFound = "AQ_WORKSTATION_NOT_FOUND";
    public const string WorkstationInactive = "AQ_WORKSTATION_INACTIVE";
    public const string WorkstationLoketConflict = "AQ_WORKSTATION_LOKET_CONFLICT";
    public const string DisplayNotFound = "AQ_DISPLAY_NOT_FOUND";
    public const string DisplayInactive = "AQ_DISPLAY_INACTIVE";
    public const string DisplayMappingRequired = "AQ_DISPLAY_MAPPING_REQUIRED";
    public const string KioskNotFound = "AQ_KIOSK_NOT_FOUND";
    public const string KioskInactive = "AQ_KIOSK_INACTIVE";
    public const string KioskMappingRequired = "AQ_KIOSK_MAPPING_REQUIRED";
    public const string Concurrency = "AQ_CONFIG_CONCURRENCY";
}
