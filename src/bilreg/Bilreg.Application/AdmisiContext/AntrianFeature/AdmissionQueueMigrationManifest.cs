namespace Bilreg.Application.AdmisiContext.AntrianFeature;

/// <summary>
/// Single source of truth for Admission Queue R-06–R-13 migration script order,
/// required tables, and critical indexes used by the real-SQL fixture and rollout preflight.
/// </summary>
public static class AdmissionQueueMigrationManifest
{
    public sealed record Script(string RelativePath, string? GuardTable);

    public static IReadOnlyList<Script> Scripts { get; } =
    [
        new("AntrianFeature/BILRG_Antrian.sql", "BILRG_Antrian"),
        new("AntrianFeature/BILRG_AntrianEntry.sql", "BILRG_AntrianEntry"),
        new("AntrianFeature/BILRG_Antrian_M1_ServicePointCode_Alter.sql", null),
        new("AntrianFeature/BILRG_Antrian_M2_QueuePrefixSnapshot_Alter.sql", null),
        new("AntrianFeature/BILRG_AntrianEntry_M2_QueueOperations_Alter.sql", null),
        new("AntrianFeature/BILRG_AdmServicePoint.sql", "BILRG_AdmServicePoint"),
        new("AntrianFeature/BILRG_AdmLoketCurrentCall.sql", "BILRG_AdmLoketCurrentCall"),
        new("RegFeature/BILRG_RegOutcome.sql", "BILRG_RegOutcome"),
        new("AntrianFeature/BILRG_AdmBookingAssistance.sql", "BILRG_AdmBookingAssistance"),
        new("AntrianFeature/BILRG_AdmissionQueue_M3_Audit_Alter.sql", null),
        new("AntrianFeature/BILRG_AdmWorkstation.sql", "BILRG_AdmWorkstation"),
        new("AntrianFeature/BILRG_AdmQueueDisplay.sql", "BILRG_AdmQueueDisplay"),
        new("AntrianFeature/BILRG_AdmDisplayLoket.sql", "BILRG_AdmDisplayLoket"),
    ];

    public static IReadOnlyList<string> RequiredTables { get; } =
    [
        "BILRG_Antrian",
        "BILRG_AntrianEntry",
        "BILRG_AdmServicePoint",
        "BILRG_AdmLoketCurrentCall",
        "BILRG_RegOutcome",
        "BILRG_AdmBookingAssistance",
        "BILRG_AdmWorkstation",
        "BILRG_AdmQueueDisplay",
        "BILRG_AdmDisplayLoket",
    ];

    public static IReadOnlyList<RequiredIndex> RequiredIndexes { get; } =
    [
        new("UX_BILRG_Antrian_SequenceTag", "BILRG_Antrian"),
        new("UX_BILRG_AdmLoketCurrentCall_ActiveEntry", "BILRG_AdmLoketCurrentCall"),
        new("IX_BILRG_AdmLoketCurrentCall_ActiveDisplay", "BILRG_AdmLoketCurrentCall"),
        new("IX_BILRG_AntrianEntry_OperationalWorklist", "BILRG_AntrianEntry"),
    ];

    public sealed record RequiredIndex(string IndexName, string TableName);
}
