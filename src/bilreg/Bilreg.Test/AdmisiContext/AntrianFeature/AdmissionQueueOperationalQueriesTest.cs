using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionQueueOperationalQueriesTest
{
    [Fact]
    public void WorklistContract_ContainsOnlyQueueOwnedFields()
    {
        var names = typeof(AdmissionQueueWorklistItem).GetProperties().Select(x => x.Name).ToArray();
        names.Should().Contain(["Priority", "QueueLabel", "ServicePointId", "QueueStatus"]);
        names.Should().NotContain(["PatientName", "PhysicianName", "RegistrationId", "Eligibility"]);
    }

    [Fact]
    public void DefaultOrdering_SurfacesPriorityButDoesNotRemoveOrSelectEntries()
    {
        var at = new DateTime(2026, 7, 23, 8, 0, 0);
        var normal = Item(1, false, at);
        var priority = Item(2, true, at.AddMinutes(1));

        var ordered = AdmissionQueueWorklistOrdering.Apply([normal, priority]).ToList();

        ordered.Select(x => x.NoUrut).Should().Equal(2, 1);
        ordered.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(0, 1, false)]
    [InlineData(1, 1, false)]
    [InlineData(2, 1, true)]
    [InlineData(2, 3, false)]
    public void AudioPlaysOnlyForNewerReloadedVersion(long reloaded, long processed, bool expected) =>
        QueueAnnouncementPolicy.ShouldPlay(reloaded, processed).Should().Be(expected);

    [Fact]
    public async Task SnapshotQuery_AlwaysReloadsProjection_ForReconnectPollingAndRefresh()
    {
        var projection = new Mock<IAdmissionQueueOperationalProjection>();
        projection.Setup(x => x.ListCurrentLoket("L1")).Returns([]);
        var sut = new CurrentLoketDisplaySnapshotHandler(projection.Object);

        await sut.Handle(new CurrentLoketDisplaySnapshotQry("L1"), CancellationToken.None);
        await sut.Handle(new CurrentLoketDisplaySnapshotQry("L1"), CancellationToken.None);
        await sut.Handle(new CurrentLoketDisplaySnapshotQry("L1"), CancellationToken.None);

        projection.Verify(x => x.ListCurrentLoket("L1"), Times.Exactly(3));
    }

    private static AdmissionQueueWorklistItem Item(int noUrut, bool priority, DateTime createdAt) =>
        new("Q", noUrut, $"A{noUrut:D4}", "ADM", "Admission", 0, priority,
            AdmissionQueueCreationReason.Normal, 0, null, null, createdAt, null, null, null);
}
