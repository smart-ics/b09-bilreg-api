using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;

namespace Bilreg.Test.BedUsageContext.WaitingListFeature;

public class WaitingListModelTest
{
    private static PasienReff SamplePasien() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");

    private static KelasReff SampleKelas() => new("K1", "Kelas 1");

    private static BangsalReff SampleBangsal() => new("B1", "Bangsal A");

    private static LayananReff SampleReferringUnit() => new("LAY01", "IGD");

    private static DokterReff SampleReferringDoctor() => new("D001", "dr. Sp.PD");

    private static BedReff SampleBed() => new("BED01", "Bed 101", true);

    private static WaitingListModel CreateWaiting() =>
        WaitingListModel.Create(
            "RG00000001",
            SamplePasien(),
            SampleKelas(),
            SampleBangsal(),
            ArrivalSourceTypeEnum.Igd,
            SampleReferringUnit(),
            SampleReferringDoctor(),
            ClinicalUrgencyEnum.Urgent,
            "ISOLASI,O2",
            priority: 1,
            "user1",
            new DateTime(2026, 10, 17, 8, 0, 0));

    #region CREATION TESTS

    [Fact]
    public void GivenValidParameters_WhenCreate_ThenCreatesActiveWaitingListWithAllProperties()
    {
        var created = CreateWaiting();

        created.WaitingListId.Should().StartWith("WTL");
        created.WaitingListStatus.Should().Be(WaitingListStatusEnum.Waiting);
        created.IsActive.Should().BeTrue();
        created.RegId.Should().Be("RG00000001");
        created.Pasien.PasienId.Should().Be("P001");
        created.KelasRawat.KelasId.Should().Be("K1");
        created.Bangsal.BangsalId.Should().Be("B1");
        created.ArrivalSourceType.Should().Be(ArrivalSourceTypeEnum.Igd);
        created.ReferringUnit.LayananId.Should().Be("LAY01");
        created.ReferringDoctor.DokterId.Should().Be("D001");
        created.ClinicalUrgency.Should().Be(ClinicalUrgencyEnum.Urgent);
        created.SpecialRequirements.Should().Be("ISOLASI,O2");
        created.PlannedBed.BedId.Should().Be("-");
        created.CancellationReason.Should().BeEmpty();
        created.RerouteReason.Should().BeEmpty();
        created.PreviousWaitListId.Should().BeEmpty();
        created.PlacedBedId.Should().BeEmpty();
        created.PlacedPakaiBedId.Should().BeEmpty();
        created.Priority.Should().Be(1);
        created.AuditTrail.Created.UserId.Should().Be("user1");
        created.AuditTrail.Created.Timestamp.Should().Be(new DateTime(2026, 10, 17, 8, 0, 0));
    }

    [Fact]
    public void GivenSimpleParameters_WhenCreate_ThenUsesSensibleDefaults()
    {
        var created = WaitingListModel.Create(
            "RG00000002",
            SamplePasien(),
            SampleKelas(),
            SampleBangsal(),
            priority: 2,
            auditUserId: "user2",
            createdAt: new DateTime(2026, 10, 17, 9, 0, 0));

        created.WaitingListStatus.Should().Be(WaitingListStatusEnum.Waiting);
        created.ArrivalSourceType.Should().Be(ArrivalSourceTypeEnum.Adm);
        created.ClinicalUrgency.Should().Be(ClinicalUrgencyEnum.Routine);
        created.SpecialRequirements.Should().BeEmpty();
        created.Priority.Should().Be(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenInvalidRegId_WhenCreate_ThenThrowsArgumentException(string regId)
    {
        var act = () => WaitingListModel.Create(
            regId,
            SamplePasien(),
            SampleKelas(),
            SampleBangsal(),
            1,
            "user1");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenNullPasien_WhenCreate_ThenThrowsArgumentNullException()
    {
        var act = () => WaitingListModel.Create(
            "RG00000001",
            null!,
            SampleKelas(),
            SampleBangsal(),
            1,
            "user1");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GivenNullKelas_WhenCreate_ThenThrowsArgumentNullException()
    {
        var act = () => WaitingListModel.Create(
            "RG00000001",
            SamplePasien(),
            null!,
            SampleBangsal(),
            1,
            "user1");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GivenNullBangsal_WhenCreate_ThenThrowsArgumentNullException()
    {
        var act = () => WaitingListModel.Create(
            "RG00000001",
            SamplePasien(),
            SampleKelas(),
            null!,
            1,
            "user1");

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenInvalidAuditUserId_WhenCreate_ThenThrowsArgumentException(string userId)
    {
        var act = () => WaitingListModel.Create(
            "RG00000001",
            SamplePasien(),
            SampleKelas(),
            SampleBangsal(),
            1,
            userId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenDefaultAndKey_WhenAccessed_ThenReturnsExpected()
    {
        var def = WaitingListModel.Default;
        def.WaitingListId.Should().Be("-");
        def.WaitingListStatus.Should().Be(WaitingListStatusEnum.Waiting);

        var key = WaitingListModel.Key("WTL123");
        key.WaitingListId.Should().Be("WTL123");
    }

    #endregion

    #region RESERVE PLANNED BED TESTS

    [Fact]
    public void GivenWaitingStatus_WhenReservePlannedBed_ThenTransitionsToReservedAndRecordsAudit()
    {
        var waiting = CreateWaiting();
        var occurredAt = new DateTime(2026, 10, 17, 8, 30, 0);

        var reserved = waiting.ReservePlannedBed(SampleBed(), "nurse1", occurredAt);

        reserved.WaitingListStatus.Should().Be(WaitingListStatusEnum.Reserved);
        reserved.IsActive.Should().BeTrue();
        reserved.PlannedBed.BedId.Should().Be("BED01");
        reserved.PlannedBed.BedName.Should().Be("Bed 101");
        reserved.AuditTrail.Modified.UserId.Should().Be("nurse1");
        reserved.AuditTrail.Modified.Timestamp.Should().Be(occurredAt);
    }

    [Fact]
    public void GivenReservedStatus_WhenReservePlannedBedAgain_ThenUpdatesPlannedBed()
    {
        var reserved = CreateWaiting().ReservePlannedBed(SampleBed(), "nurse1");
        var newBed = new BedReff("BED02", "Bed 102", true);

        var updated = reserved.ReservePlannedBed(newBed, "nurse2");

        updated.WaitingListStatus.Should().Be(WaitingListStatusEnum.Reserved);
        updated.PlannedBed.BedId.Should().Be("BED02");
        updated.AuditTrail.Modified.UserId.Should().Be("nurse2");
    }

    [Fact]
    public void GivenNullPlannedBed_WhenReservePlannedBed_ThenThrowsArgumentNullException()
    {
        var waiting = CreateWaiting();

        var act = () => waiting.ReservePlannedBed(null!, "nurse1");

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenBlankActorId_WhenReservePlannedBed_ThenThrowsArgumentException(string actorId)
    {
        var waiting = CreateWaiting();

        var act = () => waiting.ReservePlannedBed(SampleBed(), actorId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenReservedStatus_WhenClearPlannedBed_ThenRevertsToWaiting()
    {
        var reserved = CreateWaiting().ReservePlannedBed(SampleBed(), "nurse1");

        var cleared = reserved.ClearPlannedBed("nurse1", new DateTime(2026, 10, 17, 8, 45, 0));

        cleared.WaitingListStatus.Should().Be(WaitingListStatusEnum.Waiting);
        cleared.PlannedBed.BedId.Should().Be("-");
        cleared.IsActive.Should().BeTrue();
    }

    #endregion

    #region PLACE TESTS

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GivenWaitingOrReserved_WhenPlace_ThenTransitionsToPlacedAndRecordsBedDetails(bool isReserved)
    {
        var entry = CreateWaiting();
        if (isReserved)
            entry = entry.ReservePlannedBed(SampleBed(), "nurse1");

        var occurredAt = new DateTime(2026, 10, 17, 9, 15, 0);
        var placed = entry.Place("BED01", "PAK00000001", "nurse1", occurredAt);

        placed.WaitingListStatus.Should().Be(WaitingListStatusEnum.Placed);
        placed.IsActive.Should().BeFalse();
        placed.PlacedBedId.Should().Be("BED01");
        placed.PlacedPakaiBedId.Should().Be("PAK00000001");
        placed.AuditTrail.Modified.UserId.Should().Be("nurse1");
        placed.AuditTrail.Modified.Timestamp.Should().Be(occurredAt);
    }

    [Theory]
    [InlineData("", "PAK01", "user1")]
    [InlineData("BED01", "", "user1")]
    [InlineData("BED01", "PAK01", "")]
    public void GivenInvalidArguments_WhenPlace_ThenThrowsArgumentException(
        string bedId, string pakaiBedId, string actorId)
    {
        var entry = CreateWaiting();

        var act = () => entry.Place(bedId, pakaiBedId, actorId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenAlreadyPlaced_WhenPlaceAgain_ThenThrowsInvalidOperationException()
    {
        var placed = CreateWaiting().Place("BED01", "PAK01", "nurse1");

        var act = () => placed.Place("BED02", "PAK02", "nurse1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*harus Waiting atau Reserved*");
    }

    #endregion

    #region CANCEL TESTS

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GivenWaitingOrReserved_WhenCancel_ThenTransitionsToCancelledAndRecordsAudit(bool isReserved)
    {
        var entry = CreateWaiting();
        if (isReserved)
            entry = entry.ReservePlannedBed(SampleBed(), "nurse1");

        var occurredAt = new DateTime(2026, 10, 17, 9, 30, 0);
        var cancelled = entry.Cancel("Pasien menolak rawat inap", "admin1", occurredAt);

        cancelled.WaitingListStatus.Should().Be(WaitingListStatusEnum.Cancelled);
        cancelled.IsActive.Should().BeFalse();
        cancelled.CancellationReason.Should().Be("Pasien menolak rawat inap");
        cancelled.AuditTrail.Voided.UserId.Should().Be("admin1");
        cancelled.AuditTrail.Voided.Timestamp.Should().Be(occurredAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenBlankReason_WhenCancel_ThenThrowsArgumentException(string reason)
    {
        var entry = CreateWaiting();

        var act = () => entry.Cancel(reason, "admin1");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenBlankActorId_WhenCancel_ThenThrowsArgumentException(string actorId)
    {
        var entry = CreateWaiting();

        var act = () => entry.Cancel("Valid reason", actorId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenInactiveStatus_WhenCancel_ThenThrowsInvalidOperationException()
    {
        var placed = CreateWaiting().Place("BED01", "PAK01", "nurse1");
        var cancelled = CreateWaiting().Cancel("Alasan 1", "admin1");

        var actPlaced = () => placed.Cancel("Alasan 2", "admin1");
        var actCancelled = () => cancelled.Cancel("Alasan 3", "admin1");

        actPlaced.Should().Throw<InvalidOperationException>();
        actCancelled.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region REROUTE TESTS

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GivenWaitingOrReserved_WhenReroute_ThenTransitionsToReroutedAndRecordsReason(bool isReserved)
    {
        var entry = CreateWaiting();
        if (isReserved)
            entry = entry.ReservePlannedBed(SampleBed(), "nurse1");

        var occurredAt = new DateTime(2026, 10, 17, 10, 0, 0);
        var rerouted = entry.Reroute("Kondisi memburuk, alih ke ICU", "dr1", occurredAt);

        rerouted.WaitingListStatus.Should().Be(WaitingListStatusEnum.Rerouted);
        rerouted.IsActive.Should().BeFalse();
        rerouted.RerouteReason.Should().Be("Kondisi memburuk, alih ke ICU");
        rerouted.AuditTrail.Modified.UserId.Should().Be("dr1");
        rerouted.AuditTrail.Modified.Timestamp.Should().Be(occurredAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenBlankReason_WhenReroute_ThenThrowsArgumentException(string reason)
    {
        var entry = CreateWaiting();

        var act = () => entry.Reroute(reason, "dr1");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenBlankActorId_WhenReroute_ThenThrowsArgumentException(string actorId)
    {
        var entry = CreateWaiting();

        var act = () => entry.Reroute("Reason", actorId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenInactiveStatus_WhenReroute_ThenThrowsInvalidOperationException()
    {
        var cancelled = CreateWaiting().Cancel("Batal", "admin1");

        var act = () => cancelled.Reroute("Reroute ke bangsal lain", "dr1");

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region UPDATE TESTS

    [Fact]
    public void GivenWaiting_WhenUpdate_ThenUpdatesPropertiesAndPreservesStatus()
    {
        var waiting = CreateWaiting();
        var newKelas = new KelasReff("K2", "Kelas 2");
        var newBangsal = new BangsalReff("B2", "Bangsal B");
        var occurredAt = new DateTime(2026, 10, 17, 10, 30, 0);

        var updated = waiting.Update(
            newKelas,
            newBangsal,
            ClinicalUrgencyEnum.Emergency,
            "ISOLASI_AIRBORNE",
            priority: 5,
            actorId: "nurse1",
            occurredAt: occurredAt);

        updated.WaitingListStatus.Should().Be(WaitingListStatusEnum.Waiting);
        updated.KelasRawat.KelasId.Should().Be("K2");
        updated.Bangsal.BangsalId.Should().Be("B2");
        updated.ClinicalUrgency.Should().Be(ClinicalUrgencyEnum.Emergency);
        updated.SpecialRequirements.Should().Be("ISOLASI_AIRBORNE");
        updated.Priority.Should().Be(5);
        updated.AuditTrail.Modified.UserId.Should().Be("nurse1");
        updated.AuditTrail.Modified.Timestamp.Should().Be(occurredAt);
    }

    [Fact]
    public void GivenInactiveStatus_WhenUpdate_ThenThrowsInvalidOperationException()
    {
        var placed = CreateWaiting().Place("BED01", "PAK01", "nurse1");

        var act = () => placed.Update(
            new KelasReff("K2", "Kelas 2"),
            new BangsalReff("B2", "Bangsal B"),
            ClinicalUrgencyEnum.Routine,
            "",
            1,
            "nurse1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*harus Waiting atau Reserved*");
    }

    #endregion

    #region ENUM DEFINITION TESTS

    [Fact]
    public void CanonicalEnums_MustMatchSpecifications()
    {
        ((int)WaitingListStatusEnum.Waiting).Should().Be(0);
        ((int)WaitingListStatusEnum.Reserved).Should().Be(1);
        ((int)WaitingListStatusEnum.Placed).Should().Be(2);
        ((int)WaitingListStatusEnum.Cancelled).Should().Be(3);
        ((int)WaitingListStatusEnum.Rerouted).Should().Be(4);

        ((int)ClinicalUrgencyEnum.Routine).Should().Be(0);
        ((int)ClinicalUrgencyEnum.Urgent).Should().Be(1);
        ((int)ClinicalUrgencyEnum.Emergency).Should().Be(2);

        ((int)ArrivalSourceTypeEnum.Igd).Should().Be(0);
        ((int)ArrivalSourceTypeEnum.Rjl).Should().Be(1);
        ((int)ArrivalSourceTypeEnum.Adm).Should().Be(2);
        ((int)ArrivalSourceTypeEnum.TransferInternal).Should().Be(3);
    }

    #endregion
}
