using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionQueueGetRolloutStatusHandlerTest
{
    private readonly Mock<IAdmissionQueueRolloutRepo> _repo = new();
    private readonly Mock<IAdmissionQueueRolloutConfig> _config = new();

    [Fact]
    public async Task WhenAllTablesAndIndexesExist_ThenAllSchemaReady()
    {
        _repo.Setup(x => x.TableExists(It.IsAny<string>())).Returns(true);
        _repo.Setup(x => x.IndexExists(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _config.SetupGet(x => x.LegacyEndpointsEnabled).Returns(true);
        _config.SetupGet(x => x.SignalRRefreshEnabled).Returns(false);
        _config.SetupGet(x => x.WorkstationMappingsUnique).Returns(true);
        _config.SetupGet(x => x.WorkstationMappingCount).Returns(2);

        var status = await new AdmissionQueueGetRolloutStatusHandler(_repo.Object, _config.Object)
            .Handle(new AdmissionQueueGetRolloutStatusQry(), CancellationToken.None);

        status.AllSchemaReady.Should().BeTrue();
        status.Tables.Should().HaveCount(AdmissionQueueMigrationManifest.RequiredTables.Count);
        status.Indexes.Should().HaveCount(AdmissionQueueMigrationManifest.RequiredIndexes.Count);
        status.Tables.Should().OnlyContain(t => t.Ready);
        status.Indexes.Should().OnlyContain(i => i.Ready);
        status.LegacyEndpointsEnabled.Should().BeTrue();
        status.SignalRRefreshEnabled.Should().BeFalse();
        status.WorkstationMappingsUnique.Should().BeTrue();
        status.WorkstationMappingCount.Should().Be(2);
    }

    [Fact]
    public async Task WhenTableMissing_ThenNotAllSchemaReady()
    {
        _repo.Setup(x => x.TableExists(It.IsAny<string>())).Returns(true);
        _repo.Setup(x => x.TableExists("BILRG_AdmLoketCurrentCall")).Returns(false);
        _repo.Setup(x => x.IndexExists(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _config.SetupGet(x => x.WorkstationMappingsUnique).Returns(true);

        var status = await new AdmissionQueueGetRolloutStatusHandler(_repo.Object, _config.Object)
            .Handle(new AdmissionQueueGetRolloutStatusQry(), CancellationToken.None);

        status.AllSchemaReady.Should().BeFalse();
        status.Tables.Single(t => t.Name == "BILRG_AdmLoketCurrentCall").Ready.Should().BeFalse();
    }

    [Fact]
    public async Task WhenIndexMissing_ThenNotAllSchemaReady()
    {
        _repo.Setup(x => x.TableExists(It.IsAny<string>())).Returns(true);
        _repo.Setup(x => x.IndexExists(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _repo.Setup(x => x.IndexExists("UX_BILRG_Antrian_SequenceTag", "BILRG_Antrian")).Returns(false);
        _config.SetupGet(x => x.WorkstationMappingsUnique).Returns(true);

        var status = await new AdmissionQueueGetRolloutStatusHandler(_repo.Object, _config.Object)
            .Handle(new AdmissionQueueGetRolloutStatusQry(), CancellationToken.None);

        status.AllSchemaReady.Should().BeFalse();
        status.Indexes.Single(i => i.Name == "UX_BILRG_Antrian_SequenceTag").Ready.Should().BeFalse();
    }
}
