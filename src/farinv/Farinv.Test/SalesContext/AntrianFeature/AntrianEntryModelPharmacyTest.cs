using Bilreg.Domain.AdmisiContext.RegFeature;
using Farinv.Domain.SalesContext.AntrianFeature;
using FluentAssertions;

namespace Farinv.Test.SalesContext.AntrianFeature;

public class AntrianEntryModelPharmacyTest
{
    private static RegReff SampleReg => new("RG00000001", "PS001", "Sinta");

    [Fact]
    public void CreateIdentified_StoresTrackerIdAndTakenAt()
    {
        var takenAt = new DateTime(2025, 8, 3, 7, 40, 0);
        var entry = AntrianEntryModel.CreateIdentified(1, SampleReg, "01HXYZABCDEFGHJKMNPQRSTVWXY", takenAt);

        entry.PasienTrackerId.Should().Be("01HXYZABCDEFGHJKMNPQRSTVWXY");
        entry.TakenAt.Should().Be(takenAt);
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Taken);
        entry.ServedAt.Should().Be(new DateTime(3000, 1, 1));
    }

    [Fact]
    public void ConfirmSale_SetsServedAtAndAdvancesToPrepared()
    {
        var antrian = AntrianModel.Create(new DateOnly(2025, 8, 3), 1, "Apotek RJ");
        var takenAt = new DateTime(2025, 8, 3, 7, 40, 0);
        var servedAt = new DateTime(2025, 8, 3, 7, 51, 0);
        antrian.AddEntryByTracker(1, SampleReg, "01HXYZABCDEFGHJKMNPQRSTVWXY", takenAt);

        antrian.ConfirmPharmacySale(1, "DU-071", servedAt);

        var entry = antrian.ListEntry.Single();
        entry.ServedAt.Should().Be(servedAt);
        entry.ReffId.Should().Be("DU-071");
        entry.ReffDesc.Should().Be("PENJUALAN");
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Prepared);
    }

    [Fact]
    public void ConfirmSale_WhenCalledTwiceWithSameSale_IsIdempotent()
    {
        var antrian = AntrianModel.Create(new DateOnly(2025, 8, 3), 1, "Apotek RJ");
        antrian.AddEntryByTracker(
            1, SampleReg, "01HXYZABCDEFGHJKMNPQRSTVWXY", new DateTime(2025, 8, 3, 7, 40, 0));
        var servedAt = new DateTime(2025, 8, 3, 7, 51, 0);

        antrian.ConfirmPharmacySale(1, "DU-071", servedAt);
        antrian.ConfirmPharmacySale(1, "DU-071", servedAt);

        var entry = antrian.ListEntry.Single();
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Prepared);
        entry.ServedAt.Should().Be(servedAt);
    }

    [Fact]
    public void Deliver_AfterConfirmSale_CompletesEntry()
    {
        var antrian = AntrianModel.Create(new DateOnly(2025, 8, 3), 1, "Apotek RJ");
        antrian.AddEntryByTracker(
            1, SampleReg, "01HXYZABCDEFGHJKMNPQRSTVWXY", new DateTime(2025, 8, 3, 7, 40, 0));
        antrian.ConfirmPharmacySale(1, "DU-071", new DateTime(2025, 8, 3, 7, 51, 0));

        antrian.DeliverSlot(1);

        var entry = antrian.ListEntry.Single();
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Delivered);
        entry.DeliveredAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }
}

public class AntrianModelPharmacyTest
{
    private static RegReff SampleReg => new("RG00000001", "PS001", "Sinta");

    [Fact]
    public void AddEntryByTracker_DoesNotAppendTrackerEventSemantics()
    {
        var antrian = AntrianModel.Create(new DateOnly(2025, 8, 3), 1, "Apotek RJ");
        var takenAt = new DateTime(2025, 8, 3, 7, 40, 0);

        var entry = antrian.AddEntryByTracker(
            1, SampleReg, "01HXYZABCDEFGHJKMNPQRSTVWXY", takenAt);

        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Taken);
        entry.TakenAt.Should().Be(takenAt);
        antrian.ListEntry.Should().ContainSingle();
    }

    [Fact]
    public void AddEntryByTracker_WhenActiveEntryExists_ReturnsExisting()
    {
        var antrian = AntrianModel.Create(new DateOnly(2025, 8, 3), 1, "Apotek RJ");
        var trackerId = "01HXYZABCDEFGHJKMNPQRSTVWXY";
        var first = antrian.AddEntryByTracker(1, SampleReg, trackerId, DateTime.Now);

        var second = antrian.AddEntryByTracker(2, SampleReg, trackerId, DateTime.Now);

        second.NoAntrian.Should().Be(first.NoAntrian);
        antrian.ListEntry.Should().ContainSingle();
    }
}
