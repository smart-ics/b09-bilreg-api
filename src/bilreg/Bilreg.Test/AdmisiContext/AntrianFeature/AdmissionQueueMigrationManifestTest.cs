using Bilreg.Api.Configurations;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionQueueMigrationManifestTest
{
    [Fact]
    public void Scripts_MatchPhase1DependencyOrder()
    {
        AdmissionQueueMigrationManifest.Scripts.Select(s => s.RelativePath).Should().Equal(
            "AntrianFeature/BILRG_Antrian.sql",
            "AntrianFeature/BILRG_AntrianEntry.sql",
            "AntrianFeature/BILRG_Antrian_M1_ServicePointCode_Alter.sql",
            "AntrianFeature/BILRG_Antrian_M2_QueuePrefixSnapshot_Alter.sql",
            "AntrianFeature/BILRG_AntrianEntry_M2_QueueOperations_Alter.sql",
            "AntrianFeature/BILRG_AdmServicePoint.sql",
            "AntrianFeature/BILRG_AdmLoketCurrentCall.sql",
            "RegFeature/BILRG_RegOutcome.sql",
            "AntrianFeature/BILRG_AdmBookingAssistance.sql",
            "AntrianFeature/BILRG_AdmissionQueue_M3_Audit_Alter.sql",
            "../Shared/AuditLogFeature/BILRG_AuditLog.sql",
            "AntrianFeature/BILRG_AdmWorkstation.sql",
            "AntrianFeature/BILRG_AdmQueueDisplay.sql",
            "AntrianFeature/BILRG_AdmDisplayLoket.sql");
    }

    [Fact]
    public void RequiredTablesAndIndexes_AreNonEmpty()
    {
        AdmissionQueueMigrationManifest.RequiredTables.Should().NotBeEmpty();
        AdmissionQueueMigrationManifest.RequiredTables.Should().Contain("BILRG_AuditLog");
        AdmissionQueueMigrationManifest.RequiredIndexes.Should().Contain(i =>
            i.IndexName == "UX_BILRG_Antrian_SequenceTag");
    }
}

public class AdmissionQueueRolloutConfigTest
{
    [Fact]
    public void UniqueMappings_AreReportedWithoutExposingKeys()
    {
        var config = new AdmissionQueueRolloutConfig(Options.Create(new AdmissionQueueApiOptions
        {
            LegacyEndpointsEnabled = false,
            SignalRRefreshEnabled = true,
            Workstations =
            [
                new() { WorkstationKey = "ADM-01", LoketKey = "L1" },
                new() { WorkstationKey = "ADM-02", LoketKey = "L2" },
            ],
        }));

        config.LegacyEndpointsEnabled.Should().BeFalse();
        config.SignalRRefreshEnabled.Should().BeTrue();
        config.WorkstationMappingCount.Should().Be(2);
        config.WorkstationMappingsUnique.Should().BeTrue();
        config.GetType().GetProperties().Select(p => p.Name)
            .Should().NotContain(n => n.Contains("Key", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DuplicateLoket_IsReportedAsNotUnique()
    {
        var config = new AdmissionQueueRolloutConfig(Options.Create(new AdmissionQueueApiOptions
        {
            Workstations =
            [
                new() { WorkstationKey = "ADM-01", LoketKey = "L1" },
                new() { WorkstationKey = "ADM-02", LoketKey = "l1" },
            ],
        }));

        config.WorkstationMappingsUnique.Should().BeFalse();
    }
}
