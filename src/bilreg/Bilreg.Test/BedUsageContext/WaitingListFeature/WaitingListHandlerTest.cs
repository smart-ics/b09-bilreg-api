using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WaitingListFeature.UseCases;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.WaitingListFeature;

public class WaitingListHandlerTest
{
    private readonly Mock<IWaitingListRepo> _waitingListRepoMock = new();
    private readonly Mock<IAdmissionRepo> _admissionRepoMock = new();
    private readonly Mock<IKelasRepo> _kelasRepoMock = new();
    private readonly Mock<IBangsalRepo> _bangsalRepoMock = new();
    private readonly Mock<IBedRepo> _bedRepoMock = new();
    private readonly Mock<IWaitingListWorklistDal> _worklistDalMock = new();

    private readonly PasienReff _defaultPasien = new("P0001", "Pasien Test", new DateOnly(1990, 1, 1), "L");
    private readonly KelasType _defaultKelas = new("K01", "Kelas 1", true, KelasDkType.Default);
    private readonly BangsalType _defaultBangsal = new("B01", "Bangsal Mawar", RoomCatType.Default, new LayananReff("L01", "Layanan Mawar"));
    private readonly BedType _defaultBed = new("BED01", "Bed 01", new KamarReff("K01", "Kamar 01"), new BangsalReff("B01", "Bangsal Mawar"), true);

    public WaitingListHandlerTest()
    {
        _kelasRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKelasKey>()))
            .Returns(MayBe.From(_defaultKelas));
        _bangsalRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBangsalKey>()))
            .Returns(MayBe.From(_defaultBangsal));
        _bedRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBedKey>()))
            .Returns(MayBe.From(_defaultBed));
    }

    #region CREATE COMMAND TESTS

    [Fact]
    public async Task UT01_Create_GivenValidAdmissionAndNoActiveQueue_ThenCreatesWaitingEntry()
    {
        var admission = AdmissionModel.Admit(_defaultPasien, KelasDkType.Default, _defaultBangsal.ToReff(), null, null, "user1");
        _admissionRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == admission.RegId)))
            .Returns(MayBe.From(admission));
        _waitingListRepoMock.Setup(x => x.HasActiveByRegId(admission.RegId)).Returns(false);

        WaitingListModel? saved = null;
        _waitingListRepoMock.Setup(x => x.Save(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(m => saved = m);

        var handler = new WaitingListCreateHandler(
            _waitingListRepoMock.Object,
            _admissionRepoMock.Object,
            _kelasRepoMock.Object,
            _bangsalRepoMock.Object,
            _bedRepoMock.Object,
            TestTglJamProvider.Instance);

        var cmd = new WaitingListCreateCmd(
            admission.RegId,
            "K01",
            "B01",
            ArrivalSourceTypeEnum.Igd,
            "IGD",
            "Instalasi Gawat Darurat",
            "DOC01",
            "dr. Budi Sp.PD",
            ClinicalUrgencyEnum.Urgent,
            "ISOLASI,O2",
            "",
            "",
            1,
            "user2");

        var response = await handler.Handle(cmd, CancellationToken.None);

        response.Should().NotBeNull();
        response.WaitingListId.Should().StartWith("WTL");
        saved.Should().NotBeNull();
        saved!.WaitingListId.Should().Be(response.WaitingListId);
        saved.WaitingListStatus.Should().Be(WaitingListStatusEnum.Waiting);
        saved.ArrivalSourceType.Should().Be(ArrivalSourceTypeEnum.Igd);
        saved.ReferringUnit.LayananId.Should().Be("IGD");
        saved.ReferringDoctor.DokterId.Should().Be("DOC01");
        saved.ClinicalUrgency.Should().Be(ClinicalUrgencyEnum.Urgent);
        saved.SpecialRequirements.Should().Be("ISOLASI,O2");
        saved.PlannedBed.BedId.Should().Be("-");
    }

    [Fact]
    public async Task UT02_Create_GivenPlannedBed_ThenCreatesReservedEntry()
    {
        var admission = AdmissionModel.Admit(_defaultPasien, KelasDkType.Default, _defaultBangsal.ToReff(), null, null, "user1");
        _admissionRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == admission.RegId)))
            .Returns(MayBe.From(admission));
        _waitingListRepoMock.Setup(x => x.HasActiveByRegId(admission.RegId)).Returns(false);

        WaitingListModel? saved = null;
        _waitingListRepoMock.Setup(x => x.Save(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(m => saved = m);

        var handler = new WaitingListCreateHandler(
            _waitingListRepoMock.Object,
            _admissionRepoMock.Object,
            _kelasRepoMock.Object,
            _bangsalRepoMock.Object,
            _bedRepoMock.Object,
            TestTglJamProvider.Instance);

        var cmd = new WaitingListCreateCmd(
            admission.RegId,
            "K01",
            "B01",
            ArrivalSourceTypeEnum.Adm,
            PlannedBedId: "BED01",
            UserId: "user1");

        var response = await handler.Handle(cmd, CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.WaitingListStatus.Should().Be(WaitingListStatusEnum.Reserved);
        saved.PlannedBed.BedId.Should().Be("BED01");
    }

    [Fact]
    public async Task UT03_Create_GivenActiveQueueAlreadyExists_ThenReturnsExistingWaitingListIdIdempotently()
    {
        var admission = AdmissionModel.Admit(_defaultPasien, KelasDkType.Default, _defaultBangsal.ToReff(), null, null, "user1");
        var existing = CreateActiveWaitingList() with { WaitingListId = "WTL-EXISTING" };
        _waitingListRepoMock.Setup(x => x.HasActiveByRegId(admission.RegId)).Returns(true);
        _waitingListRepoMock.Setup(x => x.LoadActiveByRegId(admission.RegId)).Returns(MayBe.From(existing));

        var handler = new WaitingListCreateHandler(
            _waitingListRepoMock.Object,
            _admissionRepoMock.Object,
            _kelasRepoMock.Object,
            _bangsalRepoMock.Object,
            _bedRepoMock.Object,
            TestTglJamProvider.Instance);

        var response = await handler.Handle(
            new WaitingListCreateCmd(admission.RegId, "K01", "B01", UserId: "user1"),
            CancellationToken.None);

        response.Should().NotBeNull();
        response.WaitingListId.Should().Be("WTL-EXISTING");
        _waitingListRepoMock.Verify(x => x.Save(It.IsAny<WaitingListModel>()), Times.Never);
    }

    [Fact]
    public async Task UT04_Create_GivenAdmissionNotFound_ThenThrowsKeyNotFoundException()
    {
        _admissionRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<AdmissionModel>.None);

        var handler = new WaitingListCreateHandler(
            _waitingListRepoMock.Object,
            _admissionRepoMock.Object,
            _kelasRepoMock.Object,
            _bangsalRepoMock.Object,
            _bedRepoMock.Object,
            TestTglJamProvider.Instance);

        var act = async () => await handler.Handle(
            new WaitingListCreateCmd("RG999", "K01", "B01", UserId: "user1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public async Task UT05_Create_GivenCancelledAdmission_ThenThrowsInvalidOperationException()
    {
        var admission = AdmissionModel.Admit(_defaultPasien, KelasDkType.Default, _defaultBangsal.ToReff(), null, null, "user1")
            .Cancel("user1");
        _admissionRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(admission));

        var handler = new WaitingListCreateHandler(
            _waitingListRepoMock.Object,
            _admissionRepoMock.Object,
            _kelasRepoMock.Object,
            _bangsalRepoMock.Object,
            _bedRepoMock.Object,
            TestTglJamProvider.Instance);

        var act = async () => await handler.Handle(
            new WaitingListCreateCmd(admission.RegId, "K01", "B01", UserId: "user1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*belum dalam status admisi*");
    }

    #endregion

    #region UPDATE COMMAND TESTS

    [Fact]
    public async Task UT06_Update_GivenActiveWaitingList_ThenUpdatesFieldsAndSaves()
    {
        var waitingList = CreateActiveWaitingList();
        _waitingListRepoMock.Setup(x => x.LoadEntity(It.IsAny<IWaitingListKey>()))
            .Returns(MayBe.From(waitingList));

        WaitingListModel? saved = null;
        _waitingListRepoMock.Setup(x => x.Save(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(m => saved = m);

        var targetBangsal = new BangsalType("B02", "Bangsal Melati", RoomCatType.Default, new LayananReff("L02", "Melati"));
        _bangsalRepoMock.Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "B02")))
            .Returns(MayBe.From(targetBangsal));

        var handler = new WaitingListUpdateHandler(
            _waitingListRepoMock.Object,
            _kelasRepoMock.Object,
            _bangsalRepoMock.Object,
            _bedRepoMock.Object,
            TestTglJamProvider.Instance);

        var cmd = new WaitingListUpdateCmd(
            waitingList.WaitingListId,
            "K01",
            "B02",
            ClinicalUrgencyEnum.Emergency,
            "O2_SENTRAL",
            3,
            "user3");

        await handler.Handle(cmd, CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.Bangsal.BangsalId.Should().Be("B02");
        saved.ClinicalUrgency.Should().Be(ClinicalUrgencyEnum.Emergency);
        saved.SpecialRequirements.Should().Be("O2_SENTRAL");
        saved.Priority.Should().Be(3);
    }

    [Fact]
    public async Task UT07_Update_GivenPlannedBedProvided_ThenReservesPlannedBed()
    {
        var waitingList = CreateActiveWaitingList();
        _waitingListRepoMock.Setup(x => x.LoadEntity(It.IsAny<IWaitingListKey>()))
            .Returns(MayBe.From(waitingList));

        WaitingListModel? saved = null;
        _waitingListRepoMock.Setup(x => x.Save(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(m => saved = m);

        var handler = new WaitingListUpdateHandler(
            _waitingListRepoMock.Object,
            _kelasRepoMock.Object,
            _bangsalRepoMock.Object,
            _bedRepoMock.Object,
            TestTglJamProvider.Instance);

        var cmd = new WaitingListUpdateCmd(
            waitingList.WaitingListId,
            "K01",
            "B01",
            ClinicalUrgencyEnum.Routine,
            "",
            1,
            "user3",
            PlannedBedId: "BED01");

        await handler.Handle(cmd, CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.WaitingListStatus.Should().Be(WaitingListStatusEnum.Reserved);
        saved.PlannedBed.BedId.Should().Be("BED01");
    }

    [Fact]
    public async Task UT08_Update_GivenEmptyPlannedBedOnReservedQueue_ThenClearsPlannedBed()
    {
        var waitingList = CreateActiveWaitingList()
            .ReservePlannedBed(_defaultBed.ToReff(), "user1", DateTime.Now);
        _waitingListRepoMock.Setup(x => x.LoadEntity(It.IsAny<IWaitingListKey>()))
            .Returns(MayBe.From(waitingList));

        WaitingListModel? saved = null;
        _waitingListRepoMock.Setup(x => x.Save(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(m => saved = m);

        var handler = new WaitingListUpdateHandler(
            _waitingListRepoMock.Object,
            _kelasRepoMock.Object,
            _bangsalRepoMock.Object,
            _bedRepoMock.Object,
            TestTglJamProvider.Instance);

        var cmd = new WaitingListUpdateCmd(
            waitingList.WaitingListId,
            "K01",
            "B01",
            ClinicalUrgencyEnum.Routine,
            "",
            1,
            "user3",
            PlannedBedId: "");

        await handler.Handle(cmd, CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.WaitingListStatus.Should().Be(WaitingListStatusEnum.Waiting);
        saved.PlannedBed.BedId.Should().Be("-");
    }

    #endregion

    #region RESERVE COMMAND TESTS

    [Fact]
    public async Task UT09_Reserve_GivenActiveWaitingList_ThenMovesToReserved()
    {
        var waitingList = CreateActiveWaitingList();
        _waitingListRepoMock.Setup(x => x.LoadEntity(It.IsAny<IWaitingListKey>()))
            .Returns(MayBe.From(waitingList));

        WaitingListModel? saved = null;
        _waitingListRepoMock.Setup(x => x.Save(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(m => saved = m);

        var handler = new WaitingListReserveHandler(
            _waitingListRepoMock.Object,
            _bedRepoMock.Object,
            TestTglJamProvider.Instance);

        await handler.Handle(
            new WaitingListReserveCmd(waitingList.WaitingListId, "BED01", "user2"),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.WaitingListStatus.Should().Be(WaitingListStatusEnum.Reserved);
        saved.PlannedBed.BedId.Should().Be("BED01");
    }

    [Fact]
    public async Task UT10_Reserve_GivenWaitingListNotFound_ThenThrowsKeyNotFoundException()
    {
        _waitingListRepoMock.Setup(x => x.LoadEntity(It.IsAny<IWaitingListKey>()))
            .Returns(MayBe<WaitingListModel>.None);

        var handler = new WaitingListReserveHandler(
            _waitingListRepoMock.Object,
            _bedRepoMock.Object,
            TestTglJamProvider.Instance);

        var act = async () => await handler.Handle(
            new WaitingListReserveCmd("WTL999", "BED01", "user2"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    #endregion

    #region CANCEL COMMAND TESTS

    [Fact]
    public async Task UT11_Cancel_GivenActiveWaitingList_ThenCancelsWithReason()
    {
        var waitingList = CreateActiveWaitingList();
        _waitingListRepoMock.Setup(x => x.LoadEntity(It.IsAny<IWaitingListKey>()))
            .Returns(MayBe.From(waitingList));

        WaitingListModel? saved = null;
        _waitingListRepoMock.Setup(x => x.Save(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(m => saved = m);

        var handler = new WaitingListCancelHandler(
            _waitingListRepoMock.Object,
            TestTglJamProvider.Instance);

        await handler.Handle(
            new WaitingListCancelCmd(waitingList.WaitingListId, "Pasien menolak rawat inap", "user3"),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.WaitingListStatus.Should().Be(WaitingListStatusEnum.Cancelled);
        saved.CancellationReason.Should().Be("Pasien menolak rawat inap");
        saved.AuditTrail.Voided.UserId.Should().Be("user3");
    }

    [Fact]
    public async Task UT12_Cancel_GivenEmptyReason_ThenThrowsArgumentException()
    {
        var handler = new WaitingListCancelHandler(
            _waitingListRepoMock.Object,
            TestTglJamProvider.Instance);

        var act = async () => await handler.Handle(
            new WaitingListCancelCmd("WTL01", "   ", "user3"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region REJECT COMMAND TESTS

    [Fact]
    public async Task UT12A_Reject_GivenActiveWaitingList_ThenRejectsWithReason()
    {
        var waitingList = CreateActiveWaitingList();
        _waitingListRepoMock.Setup(x => x.LoadEntity(It.IsAny<IWaitingListKey>()))
            .Returns(MayBe.From(waitingList));

        WaitingListModel? saved = null;
        _waitingListRepoMock.Setup(x => x.Save(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(m => saved = m);

        var handler = new WaitingListRejectHandler(
            _waitingListRepoMock.Object,
            TestTglJamProvider.Instance);

        await handler.Handle(
            new WaitingListRejectCmd(waitingList.WaitingListId, "Kamar penuh / tidak ada bed isolasi", "user_bangsal"),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.WaitingListStatus.Should().Be(WaitingListStatusEnum.Rejected);
        saved.RejectReason.Should().Be("Kamar penuh / tidak ada bed isolasi");
        saved.AuditTrail.Modified.UserId.Should().Be("user_bangsal");
    }

    [Theory]
    [InlineData("", "Alasan", "user1")]
    [InlineData("   ", "Alasan", "user1")]
    [InlineData("WTL01", "", "user1")]
    [InlineData("WTL01", "   ", "user1")]
    [InlineData("WTL01", "Alasan", "")]
    [InlineData("WTL01", "Alasan", "   ")]
    public async Task UT12B_Reject_GivenInvalidGuardArgs_ThenThrowsArgumentException(
        string waitingListId,
        string reason,
        string userId)
    {
        var handler = new WaitingListRejectHandler(
            _waitingListRepoMock.Object,
            TestTglJamProvider.Instance);

        var act = async () => await handler.Handle(
            new WaitingListRejectCmd(waitingListId, reason, userId),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UT12C_Reject_GivenNonExistingWaitingList_ThenThrowsKeyNotFoundException()
    {
        _waitingListRepoMock.Setup(x => x.LoadEntity(It.IsAny<IWaitingListKey>()))
            .Returns(MayBe<WaitingListModel>.None);

        var handler = new WaitingListRejectHandler(
            _waitingListRepoMock.Object,
            TestTglJamProvider.Instance);

        var act = async () => await handler.Handle(
            new WaitingListRejectCmd("WTL999", "Alasan", "user1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    #endregion

    #region REROUTE COMMAND TESTS

    [Fact]
    public async Task UT13_Reroute_GivenActiveQueue_ThenClosesOldAndSpawnsChainedNewEntry()
    {
        var current = CreateActiveWaitingList();
        _waitingListRepoMock.Setup(x => x.LoadEntity(It.Is<IWaitingListKey>(k => k.WaitingListId == current.WaitingListId)))
            .Returns(MayBe.From(current));

        var targetBangsal = new BangsalType("B02", "Bangsal ICU", RoomCatType.Default, new LayananReff("L02", "ICU"));
        _bangsalRepoMock.Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "B02")))
            .Returns(MayBe.From(targetBangsal));

        var admission = AdmissionModel.Admit(_defaultPasien, KelasDkType.Default, _defaultBangsal.ToReff(), null, null, "user1") with { RegId = current.RegId };
        _admissionRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == current.RegId)))
            .Returns(MayBe.From(admission));

        AdmissionModel? savedAdmission = null;
        _admissionRepoMock.Setup(x => x.SaveChanges(It.IsAny<AdmissionModel>()))
            .Callback<AdmissionModel>(m => savedAdmission = m);

        var savedModels = new List<WaitingListModel>();
        _waitingListRepoMock.Setup(x => x.Save(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(savedModels.Add);

        var handler = new WaitingListRerouteHandler(
            _waitingListRepoMock.Object,
            _admissionRepoMock.Object,
            _bangsalRepoMock.Object,
            _kelasRepoMock.Object,
            _bedRepoMock.Object,
            TestTglJamProvider.Instance);

        var cmd = new WaitingListRerouteCmd(
            current.WaitingListId,
            "B02",
            "Kondisi memburuk perlu ICU",
            "user4",
            ClinicalUrgency: ClinicalUrgencyEnum.Emergency);

        var response = await handler.Handle(cmd, CancellationToken.None);

        response.Should().NotBeNull();
        response.PreviousWaitingListId.Should().Be(current.WaitingListId);
        response.NewWaitingListId.Should().StartWith("WTL");

        savedModels.Should().HaveCount(2);

        var oldEntry = savedModels[0];
        oldEntry.WaitingListId.Should().Be(current.WaitingListId);
        oldEntry.WaitingListStatus.Should().Be(WaitingListStatusEnum.Rerouted);
        oldEntry.RerouteReason.Should().Be("Kondisi memburuk perlu ICU");

        var newEntry = savedModels[1];
        newEntry.WaitingListId.Should().Be(response.NewWaitingListId);
        newEntry.WaitingListStatus.Should().Be(WaitingListStatusEnum.Waiting);
        newEntry.Bangsal.BangsalId.Should().Be("B02");
        newEntry.ClinicalUrgency.Should().Be(ClinicalUrgencyEnum.Emergency);
        newEntry.PreviousWaitListId.Should().Be(current.WaitingListId);

        savedAdmission.Should().NotBeNull();
        savedAdmission!.AdmissionStatus.Should().Be(AdmissionStatusEnum.Updated);
        savedAdmission.Bangsal.BangsalId.Should().Be("B02");
        savedAdmission.AuditTrail.Modified.UserId.Should().Be("user4");
    }

    [Fact]
    public async Task UT13b_Reroute_WithTargetKelas_ThenUpdatesAdmissionWithNewBangsalAndKelas()
    {
        var current = CreateActiveWaitingList();
        _waitingListRepoMock.Setup(x => x.LoadEntity(It.Is<IWaitingListKey>(k => k.WaitingListId == current.WaitingListId)))
            .Returns(MayBe.From(current));

        var targetBangsal = new BangsalType("B02", "Bangsal ICU", RoomCatType.Default, new LayananReff("L02", "ICU"));
        _bangsalRepoMock.Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "B02")))
            .Returns(MayBe.From(targetBangsal));

        var targetKelas = new KelasType("K02", "Kelas 2", true, new KelasDkType("DK02", "Kelas 2"));
        _kelasRepoMock.Setup(x => x.LoadEntity(It.Is<IKelasKey>(k => k.KelasId == "K02")))
            .Returns(MayBe.From(targetKelas));

        var admission = AdmissionModel.Admit(_defaultPasien, KelasDkType.Default, _defaultBangsal.ToReff(), null, null, "user1") with { RegId = current.RegId };
        _admissionRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == current.RegId)))
            .Returns(MayBe.From(admission));

        AdmissionModel? savedAdmission = null;
        _admissionRepoMock.Setup(x => x.SaveChanges(It.IsAny<AdmissionModel>()))
            .Callback<AdmissionModel>(m => savedAdmission = m);

        var handler = new WaitingListRerouteHandler(
            _waitingListRepoMock.Object,
            _admissionRepoMock.Object,
            _bangsalRepoMock.Object,
            _kelasRepoMock.Object,
            _bedRepoMock.Object,
            TestTglJamProvider.Instance);

        var cmd = new WaitingListRerouteCmd(
            current.WaitingListId,
            "B02",
            "Pindah bangsal dan upgrade kelas",
            "user5",
            TargetKelasId: "K02");

        var response = await handler.Handle(cmd, CancellationToken.None);

        response.Should().NotBeNull();
        savedAdmission.Should().NotBeNull();
        savedAdmission!.AdmissionStatus.Should().Be(AdmissionStatusEnum.Updated);
        savedAdmission.Bangsal.BangsalId.Should().Be("B02");
        savedAdmission.KelasDk.KelasDkId.Should().Be("DK02");
        savedAdmission.AuditTrail.Modified.UserId.Should().Be("user5");
    }

    [Fact]
    public async Task UT14_Reroute_GivenEmptyReason_ThenThrowsArgumentException()
    {
        var handler = new WaitingListRerouteHandler(
            _waitingListRepoMock.Object,
            _admissionRepoMock.Object,
            _bangsalRepoMock.Object,
            _kelasRepoMock.Object,
            _bedRepoMock.Object,
            TestTglJamProvider.Instance);

        var act = async () => await handler.Handle(
            new WaitingListRerouteCmd("WTL01", "B02", "", "user1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UT14b_Reroute_WhenAdmissionNotFound_ThenThrowsKeyNotFoundException()
    {
        var current = CreateActiveWaitingList();
        _waitingListRepoMock.Setup(x => x.LoadEntity(It.Is<IWaitingListKey>(k => k.WaitingListId == current.WaitingListId)))
            .Returns(MayBe.From(current));

        var targetBangsal = new BangsalType("B02", "Bangsal ICU", RoomCatType.Default, new LayananReff("L02", "ICU"));
        _bangsalRepoMock.Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "B02")))
            .Returns(MayBe.From(targetBangsal));

        _admissionRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == current.RegId)))
            .Returns(MayBe<AdmissionModel>.None);

        var handler = new WaitingListRerouteHandler(
            _waitingListRepoMock.Object,
            _admissionRepoMock.Object,
            _bangsalRepoMock.Object,
            _kelasRepoMock.Object,
            _bedRepoMock.Object,
            TestTglJamProvider.Instance);

        var act = async () => await handler.Handle(
            new WaitingListRerouteCmd(current.WaitingListId, "B02", "Alasan reroute", "user1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*Admission '{current.RegId}' tidak ditemukan.*");
    }

    #endregion

    #region GET QUERY TESTS

    [Fact]
    public async Task UT15_Get_GivenExistingId_ThenReturnsDetailResponse()
    {
        var waitingList = CreateActiveWaitingList();
        _waitingListRepoMock.Setup(x => x.LoadEntity(It.Is<IWaitingListKey>(k => k.WaitingListId == waitingList.WaitingListId)))
            .Returns(MayBe.From(waitingList));

        var handler = new WaitingListGetHandler(_waitingListRepoMock.Object);

        var response = await handler.Handle(new WaitingListGetQuery(waitingList.WaitingListId), CancellationToken.None);

        response.Should().NotBeNull();
        response.WaitingListId.Should().Be(waitingList.WaitingListId);
        response.RegId.Should().Be(waitingList.RegId);
        response.Pasien.PasienId.Should().Be(waitingList.Pasien.PasienId);
        response.Bangsal.BangsalId.Should().Be(waitingList.Bangsal.BangsalId);
        response.WaitingListStatus.Should().Be(WaitingListStatusEnum.Waiting);
    }

    [Fact]
    public async Task UT16_Get_GivenNotFound_ThenThrowsKeyNotFoundException()
    {
        _waitingListRepoMock.Setup(x => x.LoadEntity(It.IsAny<IWaitingListKey>()))
            .Returns(MayBe<WaitingListModel>.None);

        var handler = new WaitingListGetHandler(_waitingListRepoMock.Object);

        var act = async () => await handler.Handle(new WaitingListGetQuery("WTL999"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    #endregion

    #region GET BY REG QUERY TESTS

    [Fact]
    public async Task UT17_GetByReg_GivenActiveQueueExists_ThenReturnsActiveEntry()
    {
        var active = CreateActiveWaitingList() with { WaitingListId = "WTL-ACTIVE" };

        _waitingListRepoMock.Setup(x => x.LoadActiveByRegId("RG001"))
            .Returns(MayBe.From(active));

        var handler = new WaitingListGetByRegHandler(_waitingListRepoMock.Object);

        var response = await handler.Handle(new WaitingListGetByRegQuery("RG001"), CancellationToken.None);

        response.Should().NotBeNull();
        response!.WaitingListId.Should().Be("WTL-ACTIVE");
    }

    [Fact]
    public async Task UT18_GetByReg_GivenNoActiveQueue_WhenHistoryExists_ThenReturnsLatestEntry()
    {
        var historical = CreateActiveWaitingList() with { WaitingListId = "WTL-HISTORICAL" };

        _waitingListRepoMock.Setup(x => x.LoadActiveByRegId("RG001"))
            .Returns(MayBe<WaitingListModel>.None);
        _waitingListRepoMock.Setup(x => x.ListByRegId("RG001"))
            .Returns(new[] { historical });

        var handler = new WaitingListGetByRegHandler(_waitingListRepoMock.Object);

        var response = await handler.Handle(new WaitingListGetByRegQuery("RG001"), CancellationToken.None);

        response.Should().NotBeNull();
        response!.WaitingListId.Should().Be("WTL-HISTORICAL");
    }

    [Fact]
    public async Task UT18B_GetByReg_GivenNoEntries_ThenReturnsNull()
    {
        _waitingListRepoMock.Setup(x => x.LoadActiveByRegId("RG001"))
            .Returns(MayBe<WaitingListModel>.None);
        _waitingListRepoMock.Setup(x => x.ListByRegId("RG001"))
            .Returns(Enumerable.Empty<WaitingListModel>());

        var handler = new WaitingListGetByRegHandler(_waitingListRepoMock.Object);

        var response = await handler.Handle(new WaitingListGetByRegQuery("RG001"), CancellationToken.None);

        response.Should().BeNull();
    }

    #endregion

    #region WORKLIST QUERY TESTS

    [Fact]
    public async Task UT19_Worklist_GivenBangsalId_ThenReturnsItemsFromDal()
    {
        var items = new List<WaitingListWorklistView>
        {
            new("WTL01", "RG001", 0, 1, "P01", "Pasien A", "L", "1990-01-01", "K01", "Kelas 1", "B01", "Bangsal A", 0, "IGD", "IGD", "DOC1", "dr. Budi", 2, "ISOLASI", "", "", "", "user1", DateTime.Now),
            new("WTL02", "RG002", 0, 1, "P02", "Pasien B", "P", "1995-05-05", "K01", "Kelas 1", "B01", "Bangsal A", 1, "POLI", "Poli Penyakit Dalam", "DOC2", "dr. Ani", 0, "", "", "", "", "user2", DateTime.Now)
        };

        _worklistDalMock.Setup(x => x.ListActiveWorklist("B01"))
            .Returns(items);

        var handler = new WaitingListWorklistHandler(_worklistDalMock.Object);

        var response = await handler.Handle(new WaitingListWorklistQuery("B01"), CancellationToken.None);

        response.Should().HaveCount(2);
        response.First().ClinicalUrgency.Should().Be(2);
    }

    [Fact]
    public async Task UT20_Worklist_GivenEmptyBangsalId_ThenThrowsArgumentException()
    {
        var handler = new WaitingListWorklistHandler(_worklistDalMock.Object);

        var act = async () => await handler.Handle(new WaitingListWorklistQuery(""), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region LIST QUERY TESTS

    [Fact]
    public async Task UT21_List_GivenFilter_ThenReturnsEnvelopeWithItems()
    {
        var items = new List<WaitingListWorklistView>
        {
            new("WTL01", "RG001", 0, 1, "P01", "Pasien A", "L", "1990-01-01", "K01", "Kelas 1", "B01", "Bangsal A", 0, "IGD", "IGD", "DOC1", "dr. Budi", 2, "ISOLASI", "", "", "", "user1", DateTime.Now)
        };

        _worklistDalMock.Setup(x => x.List(It.Is<WaitingListWorklistFilter>(f => f.BangsalId == "B01" && f.WaitingListStatus == 0)))
            .Returns(items);

        var handler = new WaitingListListHandler(_worklistDalMock.Object);

        var response = await handler.Handle(new WaitingListListQuery("B01", 0), CancellationToken.None);

        response.Should().NotBeNull();
        response.Items.Should().HaveCount(1);
        response.Items.First().WaitingListId.Should().Be("WTL01");
    }

    [Fact]
    public async Task UT22_List_GivenNullFilter_ThenPassesNullFilterToDalAndReturnsEnvelope()
    {
        var items = new List<WaitingListWorklistView>
        {
            new("WTL01", "RG001", 0, 1, "P01", "Pasien A", "L", "1990-01-01", "K01", "Kelas 1", "B01", "Bangsal A", 0, "IGD", "IGD", "DOC1", "dr. Budi", 2, "ISOLASI", "", "", "", "user1", DateTime.Now)
        };

        _worklistDalMock.Setup(x => x.List(It.Is<WaitingListWorklistFilter>(f => f.BangsalId == null && f.WaitingListStatus == null)))
            .Returns(items);

        var handler = new WaitingListListHandler(_worklistDalMock.Object);

        var response = await handler.Handle(new WaitingListListQuery(null, null), CancellationToken.None);

        response.Should().NotBeNull();
        response.Items.Should().HaveCount(1);
        response.Items.First().WaitingListId.Should().Be("WTL01");
    }

    #endregion

    private WaitingListModel CreateActiveWaitingList() =>
        WaitingListModel.Create(
            "RG00000001",
            _defaultPasien,
            _defaultKelas.ToReff(),
            _defaultBangsal.ToReff(),
            ArrivalSourceTypeEnum.Igd,
            new LayananReff("IGD", "Instalasi Gawat Darurat"),
            new DokterReff("DOC01", "dr. Budi"),
            ClinicalUrgencyEnum.Routine,
            "",
            1,
            "user1",
            DateTime.Now);
}
