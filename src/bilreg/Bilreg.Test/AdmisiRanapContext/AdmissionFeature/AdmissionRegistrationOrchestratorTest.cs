using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.AdmisiRanapContext;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Application.AdmisiRanapContext.ReservationFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Application.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using MyHospital.MsgContract.Billing.AdmisiEvents;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmissionRegistrationOrchestratorTest
{
    [Fact]
    public async Task GivenOpnameRequest_WhenProcessed_ThenCreatesAdmissionRegRegInapAndRegAktifWithSharedId()
    {
        var harness = CreateOpnameHarness();
        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2", RegistrationData());

        var response = await harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        harness.SavedAdmission.Should().NotBeNull();
        harness.SavedReg.Should().NotBeNull();
        harness.SavedRegInap.Should().NotBeNull();
        harness.SavedRegAktif.Should().NotBeNull();
        harness.SavedAdmission!.AdmissionSource.Should().Be(AdmissionSourceEnum.Admission);
        harness.SavedReg!.JenisReg.Should().Be(JenisRegEnum.RegInap);
        harness.SavedReg.RegId.Should().Be(harness.SavedAdmission.RegId).And.Be(response.RegId);
        harness.SavedReg.Layanan.LayananId.Should().Be("RI1");
        harness.SavedReg.Karcis.KarcisId.Should().Be(KarcisType.Default.KarcisId);
        harness.SavedReg.ListKomponen.Should().BeEmpty();
        harness.SavedRegInap!.RegId.Should().Be(response.RegId);
        harness.SavedRegInap.ProsedurMasukInap.ProsedurMasukInapId.Should().Be("IGD");
        harness.SavedRegInap.Dpjp.PpaId.Should().Be(harness.SavedReg.Dokter.PpaId);
        harness.SavedRegAktif!.RegId.Should().Be(response.RegId);
        harness.SavedOpname!.FulfilledRegId.Should().Be(response.RegId);
        harness.ProsedurRepo.Verify(
            x => x.LoadEntity(It.Is<IProsedurMasukInapKey>(k => k.ProsedurMasukInapId == "IGD")),
            Times.Once);
        harness.BangsalRepo.Verify(
            x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "B1")),
            Times.Once);
        harness.LayananRepo.Verify(
            x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "RI1")),
            Times.Once);
        harness.RegInapRepo.Verify(x => x.SaveChanges(It.IsAny<RegInapModel>()), Times.Once);
        harness.Publisher.Verify(
            x => x.PublishRanapCreatedAsync(response.RegId, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task GivenReservation_WhenProcessed_ThenCreatesRegInapWithSharedIdAndSelectedDoctor()
    {
        var harness = CreateReservationHarness();
        var command = new AdmProcessReservationCmd(
            harness.Reservation.ReservationId, "1", "B1", "user2", RegistrationData());

        var response = await harness.Sut.ProcessReservation(command, CancellationToken.None);

        response.RegId.Should().NotBeNullOrWhiteSpace();
        harness.SavedRegInap.Should().NotBeNull();
        harness.SavedRegInap!.RegId.Should().Be(response.RegId);
        harness.SavedRegInap.ProsedurMasukInap.ProsedurMasukInapId.Should().Be("IGD");
        harness.SavedRegInap.Dpjp.Should().NotBeNull();
        harness.SavedRegInap.ListDokter.Should().ContainSingle(x =>
            x.IsActive
            && x.DokterRole == DokterRoleEnum.Dpjp
            && x.DpjpResponsibility == DpjpResponsibilityEnum.Primary);
        harness.ProsedurRepo.Verify(
            x => x.LoadEntity(It.Is<IProsedurMasukInapKey>(k => k.ProsedurMasukInapId == "IGD")),
            Times.Once);
        harness.AdmissionRepo.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Once);
        harness.RegInapRepo.Verify(x => x.SaveChanges(It.IsAny<RegInapModel>()), Times.Once);
        harness.Publisher.Verify(
            x => x.PublishRanapCreatedAsync(response.RegId, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task GivenBangsalWithoutLayananMapping_WhenProcessed_ThenRejectsBeforePersistence()
    {
        var harness = CreateOpnameHarness();
        harness.BangsalRepo
            .Setup(x => x.LoadEntity(It.IsAny<IBangsalKey>()))
            .Returns(MayBe.From(new BangsalType("B1", "Bangsal 1", RoomCatType.Default, new LayananReff("-", "-"))));
        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2", RegistrationData());

        var act = () => harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak memiliki mapping Layanan Rawat Inap*");
        harness.AdmissionRepo.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Never);
        harness.RegRepo.Verify(x => x.SaveChanges(It.IsAny<RegModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenBangsalMappedToNonInpatientLayanan_WhenProcessed_ThenRejectsBeforePersistence()
    {
        var harness = CreateOpnameHarness();
        var layanan = LayananType.Default with
        {
            LayananId = "RJ1",
            LayananName = "Poli",
            InstalasiDk = InstalasiDkType.RawatJalan
        };
        harness.BangsalRepo
            .Setup(x => x.LoadEntity(It.IsAny<IBangsalKey>()))
            .Returns(MayBe.From(new BangsalType("B1", "Bangsal 1", RoomCatType.Default, layanan.ToReff())));
        harness.LayananRepo
            .Setup(x => x.LoadEntity(It.IsAny<ILayananKey>()))
            .Returns(MayBe.From(layanan));
        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2", RegistrationData());

        var act = () => harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*bukan instalasi rawat inap*");
        harness.AdmissionRepo.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Never);
        harness.RegRepo.Verify(x => x.SaveChanges(It.IsAny<RegModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenMissingProsedurMasukInapId_WhenProcessed_ThenRejectsBeforePersistence()
    {
        var harness = CreateOpnameHarness();
        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2",
            RegistrationData() with { ProsedurMasukInapId = "   " });

        var act = () => harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        harness.AdmissionRepo.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Never);
        harness.RegRepo.Verify(x => x.SaveChanges(It.IsAny<RegModel>()), Times.Never);
        harness.RegInapRepo.Verify(x => x.SaveChanges(It.IsAny<RegInapModel>()), Times.Never);
        harness.ProsedurRepo.Verify(x => x.LoadEntity(It.IsAny<IProsedurMasukInapKey>()), Times.Never);
    }

    [Fact]
    public async Task GivenUnknownProsedurMasukInapId_WhenProcessed_ThenRejectsBeforePersistence()
    {
        var harness = CreateOpnameHarness();
        harness.ProsedurRepo
            .Setup(x => x.LoadEntity(It.IsAny<IProsedurMasukInapKey>()))
            .Returns(MayBe<ProsedurMasukInapType>.None);
        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2",
            RegistrationData() with { ProsedurMasukInapId = "XXX" });

        var act = () => harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*Prosedur Masuk Inap*XXX*tidak ditemukan*");
        harness.AdmissionRepo.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Never);
        harness.RegRepo.Verify(x => x.SaveChanges(It.IsAny<RegModel>()), Times.Never);
        harness.RegInapRepo.Verify(x => x.SaveChanges(It.IsAny<RegInapModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenRegInapPersistenceFails_WhenProcessed_ThenRollsBackCompleteWorkflow()
    {
        var harness = CreateOpnameHarness();
        harness.RegInapRepo
            .Setup(x => x.SaveChanges(It.IsAny<RegInapModel>()))
            .Throws(new InvalidOperationException("forced ta_reg_inap failure"));
        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2", RegistrationData());

        var act = () => harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*forced ta_reg_inap failure*");
        harness.SavedRegAktif.Should().BeNull();
        harness.SavedOpname.Should().BeNull();
        harness.RegAktifRepo.Verify(x => x.SaveChanges(It.IsAny<RegAktifModel>()), Times.Never);
        harness.OpnameRepo.Verify(x => x.SaveChanges(It.IsAny<OpnameRequestModel>()), Times.Never);
        harness.AuditRepo.Verify(x => x.SaveChanges(It.IsAny<Domain.Shared.AuditLogFeature.AuditLog>()), Times.Never);
        harness.Publisher.Verify(x => x.PublishRanapCreatedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenDatangSendiriWithoutRujukan_WhenProcessed_ThenPersistsDefaultRujukanWithoutLoadingRepo()
    {
        var harness = CreateOpnameHarness();
        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2",
            RegistrationData() with { CaraMasukDkId = CaraMasukDkType.DatangSendiri.CaraMasukDkId, RujukanId = null });

        var response = await harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        response.RegId.Should().NotBeNullOrWhiteSpace();
        harness.SavedReg.Should().NotBeNull();
        harness.SavedReg!.Rujukan.RujukanId.Should().Be(RujukanType.Default.RujukanId);
        harness.SavedReg.CaraMasukDk.CaraMasukDkId.Should().Be(CaraMasukDkType.DatangSendiri.CaraMasukDkId);
        harness.RujukanRepo.Verify(x => x.LoadEntity(It.IsAny<IRujukanKey>()), Times.Never);
    }

    [Fact]
    public async Task GivenCaraMasukRequiresRujukanWithoutRujukanId_WhenProcessed_ThenRejectsBeforePersistence()
    {
        var harness = CreateOpnameHarness();
        harness.CaraMasukRepo
            .Setup(x => x.LoadEntity(It.IsAny<ICaraMasukDkKey>()))
            .Returns(MayBe.From(CaraMasukDkType.RujukanRs));
        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2",
            RegistrationData() with
            {
                CaraMasukDkId = CaraMasukDkType.RujukanRs.CaraMasukDkId,
                RujukanId = null
            });

        var act = () => harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        harness.AdmissionRepo.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Never);
        harness.RegRepo.Verify(x => x.SaveChanges(It.IsAny<RegModel>()), Times.Never);
        harness.RujukanRepo.Verify(x => x.LoadEntity(It.IsAny<IRujukanKey>()), Times.Never);
    }

    [Fact]
    public async Task GivenCaraMasukRequiresRujukanWithValidRujukan_WhenProcessed_ThenLoadsAndPersistsRujukan()
    {
        var harness = CreateOpnameHarness();
        var rujukan = RujukanType.Default with
        {
            RujukanId = "RJ1",
            RujukanName = "RS Rujukan",
            CaraMasukDk = CaraMasukDkType.RujukanRs
        };
        harness.CaraMasukRepo
            .Setup(x => x.LoadEntity(It.IsAny<ICaraMasukDkKey>()))
            .Returns(MayBe.From(CaraMasukDkType.RujukanRs));
        harness.RujukanRepo
            .Setup(x => x.LoadEntity(It.IsAny<IRujukanKey>()))
            .Returns(MayBe.From(rujukan));
        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2",
            RegistrationData() with
            {
                CaraMasukDkId = CaraMasukDkType.RujukanRs.CaraMasukDkId,
                RujukanId = "RJ1"
            });

        var response = await harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        response.RegId.Should().NotBeNullOrWhiteSpace();
        harness.SavedReg.Should().NotBeNull();
        harness.SavedReg!.Rujukan.RujukanId.Should().Be("RJ1");
        harness.RujukanRepo.Verify(
            x => x.LoadEntity(It.Is<IRujukanKey>(k => k.RujukanId == "RJ1")),
            Times.Once);
    }

    [Fact]
    public async Task GivenOpnameRequest_WhenSucceeds_ThenPublishesRanapCreatedEventWithRegId()
    {
        var harness = CreateOpnameHarness();
        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2", RegistrationData());

        var response = await harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        harness.Publisher.Verify(
            x => x.PublishRanapCreatedAsync(response.RegId, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task GivenReservation_WhenSucceeds_ThenPublishesRanapCreatedEventWithRegId()
    {
        var harness = CreateReservationHarness();
        var command = new AdmProcessReservationCmd(
            harness.Reservation.ReservationId, "1", "B1", "user2", RegistrationData());

        var response = await harness.Sut.ProcessReservation(command, CancellationToken.None);

        harness.Publisher.Verify(
            x => x.PublishRanapCreatedAsync(response.RegId, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task GivenReservation_WhenPersistenceFails_ThenDoesNotPublishRanapCreatedEvent()
    {
        var harness = CreateReservationHarness();
        harness.RegInapRepo
            .Setup(x => x.SaveChanges(It.IsAny<RegInapModel>()))
            .Throws(new InvalidOperationException("forced reservation failure"));
        var command = new AdmProcessReservationCmd(
            harness.Reservation.ReservationId, "1", "B1", "user2", RegistrationData());

        var act = () => harness.Sut.ProcessReservation(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        harness.Publisher.Verify(
            x => x.PublishRanapCreatedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenOpnameRequest_WhenRabbitMqBrokerFails_ThenRegistrationStillSucceedsWithoutThrowing()
    {
        var failingBus = new Mock<IBus>();
        failingBus.Setup(x => x.Publish(It.IsAny<RegRanapCreatedNotifEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RabbitMQ connection down"));
        var mockLogger = new Mock<ILogger<AdmisiEventPublisher>>();
        var realPublisher = new AdmisiEventPublisher(failingBus.Object, mockLogger.Object);

        var harness = CreateOpnameHarness(realPublisher);
        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2", RegistrationData());

        var response = await harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        response.Should().NotBeNull();
        response.RegId.Should().NotBeNullOrWhiteSpace();
        failingBus.Verify(x => x.Publish(It.IsAny<RegRanapCreatedNotifEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        mockLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task GivenReservation_WhenRabbitMqBrokerFails_ThenRegistrationStillSucceedsWithoutThrowing()
    {
        var failingBus = new Mock<IBus>();
        failingBus.Setup(x => x.Publish(It.IsAny<RegRanapCreatedNotifEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RabbitMQ connection down"));
        var mockLogger = new Mock<ILogger<AdmisiEventPublisher>>();
        var realPublisher = new AdmisiEventPublisher(failingBus.Object, mockLogger.Object);

        var harness = CreateReservationHarness(realPublisher);
        var command = new AdmProcessReservationCmd(
            harness.Reservation.ReservationId, "1", "B1", "user2", RegistrationData());

        var response = await harness.Sut.ProcessReservation(command, CancellationToken.None);

        response.Should().NotBeNull();
        response.RegId.Should().NotBeNullOrWhiteSpace();
        failingBus.Verify(x => x.Publish(It.IsAny<RegRanapCreatedNotifEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        mockLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    private static AdmissionRegistrationData RegistrationData() =>
        new("00000", CaraMasukDkType.DatangSendiri.CaraMasukDkId, "IGD", null, "-", "PESERTA1");

    [Fact]
    public async Task GivenOpnameRequest_WhenWaitingListPlacementSucceeds_ThenCreatesWaitingListAndReturnsId()
    {
        var harness = CreateOpnameHarness();
        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2", RegistrationData());

        var response = await harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        response.WaitingListId.Should().NotBeNullOrWhiteSpace();
        harness.SavedWaitingList.Should().NotBeNull();
        harness.SavedWaitingList!.WaitingListId.Should().Be(response.WaitingListId);
        harness.SavedWaitingList.RegId.Should().Be(response.RegId);
        harness.SavedWaitingList.WaitingListStatus.Should().Be(WaitingListStatusEnum.Waiting);
        harness.SavedWaitingList.Priority.Should().Be(3);
        harness.SavedWaitingList.KelasRawat.KelasId.Should().Be("K1");
        harness.SavedWaitingList.Bangsal.BangsalId.Should().Be("B1");
        harness.WaitingListRepo.Verify(x => x.SaveChanges(It.IsAny<WaitingListModel>()), Times.Once);
        harness.AuditRepo.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.EntityName == nameof(WaitingListModel) && a.EntityId == response.WaitingListId)), Times.Once);
        harness.WardGateway.Verify(x => x.NotifyHandOver(It.Is<WardAccommodationHandOver>(h =>
            h.WaitingListId == response.WaitingListId &&
            h.RegId == response.RegId &&
            h.BangsalId == "B1" &&
            h.KelasId == "K1" &&
            h.Status == WaitingListStatusEnum.Waiting)), Times.Once);
    }

    [Fact]
    public async Task GivenOpnameRequest_WithExplicitKelasAndPriority_ThenPropagatesToWaitingList()
    {
        var harness = CreateOpnameHarness();
        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2", RegistrationData(),
            KelasId: "VIP", Priority: 1);

        var response = await harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        response.WaitingListId.Should().NotBeNullOrWhiteSpace();
        harness.SavedWaitingList.Should().NotBeNull();
        harness.SavedWaitingList!.Priority.Should().Be(1);
        harness.SavedWaitingList.KelasRawat.KelasId.Should().Be("VIP");
    }

    [Fact]
    public async Task GivenReservation_WhenWaitingListPlacementSucceeds_ThenCreatesWaitingListAndReturnsId()
    {
        var harness = CreateReservationHarness();
        var command = new AdmProcessReservationCmd(
            harness.Reservation.ReservationId, "1", "B1", "user2", RegistrationData());

        var response = await harness.Sut.ProcessReservation(command, CancellationToken.None);

        response.WaitingListId.Should().NotBeNullOrWhiteSpace();
        harness.SavedWaitingList.Should().NotBeNull();
        harness.SavedWaitingList!.WaitingListId.Should().Be(response.WaitingListId);
        harness.SavedWaitingList.RegId.Should().Be(response.RegId);
        harness.SavedWaitingList.Priority.Should().Be(3);
        harness.WaitingListRepo.Verify(x => x.SaveChanges(It.IsAny<WaitingListModel>()), Times.Once);
        harness.AuditRepo.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.EntityName == nameof(WaitingListModel) && a.EntityId == response.WaitingListId)), Times.Once);
        harness.WardGateway.Verify(x => x.NotifyHandOver(It.IsAny<WardAccommodationHandOver>()), Times.Once);
    }

    [Fact]
    public async Task GivenOpnameRequest_WhenWaitingListFails_ThenRegistrationSucceedsWithNullWaitingListId()
    {
        var harness = CreateOpnameHarness();
        harness.WaitingListRepo
            .Setup(x => x.SaveChanges(It.IsAny<WaitingListModel>()))
            .Throws(new InvalidOperationException("forced waiting list db error"));

        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2", RegistrationData());

        var response = await harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        response.RegId.Should().NotBeNullOrWhiteSpace();
        response.WaitingListId.Should().BeNull();
        harness.SavedAdmission.Should().NotBeNull();
        harness.SavedReg.Should().NotBeNull();
        harness.SavedRegInap.Should().NotBeNull();
        harness.SavedRegAktif.Should().NotBeNull();
        harness.Publisher.Verify(
            x => x.PublishRanapCreatedAsync(response.RegId, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task GivenReservation_WhenWaitingListFails_ThenRegistrationSucceedsWithNullWaitingListId()
    {
        var harness = CreateReservationHarness();
        harness.WaitingListRepo
            .Setup(x => x.SaveChanges(It.IsAny<WaitingListModel>()))
            .Throws(new InvalidOperationException("forced waiting list db error"));

        var command = new AdmProcessReservationCmd(
            harness.Reservation.ReservationId, "1", "B1", "user2", RegistrationData());

        var response = await harness.Sut.ProcessReservation(command, CancellationToken.None);

        response.RegId.Should().NotBeNullOrWhiteSpace();
        response.WaitingListId.Should().BeNull();
        harness.SavedRegInap.Should().NotBeNull();
        harness.Publisher.Verify(
            x => x.PublishRanapCreatedAsync(response.RegId, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task GivenOpnameRequest_WhenWardHandOverFails_ThenRegistrationSucceedsWithNullWaitingListId()
    {
        var harness = CreateOpnameHarness();
        harness.WardGateway
            .Setup(x => x.NotifyHandOver(It.IsAny<WardAccommodationHandOver>()))
            .Throws(new InvalidOperationException("handover network error"));

        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2", RegistrationData());

        var response = await harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        response.RegId.Should().NotBeNullOrWhiteSpace();
        response.WaitingListId.Should().BeNull();
        harness.SavedAdmission.Should().NotBeNull();
        harness.SavedReg.Should().NotBeNull();
        harness.SavedRegInap.Should().NotBeNull();
    }

    [Fact]
    public async Task GivenOpnameRequest_WhenProcessed_ThenRecordsAdmissionRegisteredWithRegId()
    {
        var harness = CreateOpnameHarness();
        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2", RegistrationData());

        var response = await harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        response.RegId.Should().NotBeNullOrWhiteSpace();
        harness.TrackerCoordinator.Verify(
            x => x.RecordAdmissionRegistered(harness.Opname, response.RegId, TestTglJamProvider.Instance.Now),
            Times.Once);
    }

    [Fact]
    public async Task GivenReservation_WhenProcessed_ThenRecordsAdmissionRegisteredWithRegId()
    {
        var harness = CreateReservationHarness();
        var command = new AdmProcessReservationCmd(
            harness.Reservation.ReservationId, "1", "B1", "user2", RegistrationData());

        var response = await harness.Sut.ProcessReservation(command, CancellationToken.None);

        response.RegId.Should().NotBeNullOrWhiteSpace();
        harness.TrackerCoordinator.Verify(
            x => x.RecordAdmissionRegistered(harness.Reservation, response.RegId, TestTglJamProvider.Instance.Now),
            Times.Once);
    }

    [Fact]
    public async Task GivenOpnameRequest_WhenTrackerCoordinatorThrows_ThenProcessStillSucceedsGracefully()
    {
        var trackerCoordinator = new Mock<IPasienTrackerInpatientCoordinator>();
        trackerCoordinator
            .Setup(x => x.RecordAdmissionRegistered(It.IsAny<OpnameRequestModel>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Throws(new InvalidOperationException("Tracker persistence failure"));

        var harness = CreateOpnameHarness(customTrackerCoordinator: trackerCoordinator);
        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2", RegistrationData());

        var response = await harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        response.RegId.Should().NotBeNullOrWhiteSpace();
        harness.SavedAdmission.Should().NotBeNull();
        harness.SavedReg.Should().NotBeNull();
        harness.SavedRegInap.Should().NotBeNull();
        harness.SavedOpname.Should().NotBeNull();
        trackerCoordinator.Verify(
            x => x.RecordAdmissionRegistered(harness.Opname, response.RegId, TestTglJamProvider.Instance.Now),
            Times.Once);
    }

    [Fact]
    public async Task GivenReservation_WhenTrackerCoordinatorThrows_ThenProcessStillSucceedsGracefully()
    {
        var trackerCoordinator = new Mock<IPasienTrackerInpatientCoordinator>();
        trackerCoordinator
            .Setup(x => x.RecordAdmissionRegistered(It.IsAny<ReservationModel>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Throws(new InvalidOperationException("Tracker persistence failure"));

        var harness = CreateReservationHarness(customTrackerCoordinator: trackerCoordinator);
        var command = new AdmProcessReservationCmd(
            harness.Reservation.ReservationId, "1", "B1", "user2", RegistrationData());

        var response = await harness.Sut.ProcessReservation(command, CancellationToken.None);

        response.RegId.Should().NotBeNullOrWhiteSpace();
        harness.SavedRegInap.Should().NotBeNull();
        harness.AdmissionRepo.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Once);
        trackerCoordinator.Verify(
            x => x.RecordAdmissionRegistered(harness.Reservation, response.RegId, TestTglJamProvider.Instance.Now),
            Times.Once);
    }

    #region SOP Rawat Inap - Jenis Inap & Relasi Perinatal-Obstetri Tests

    private static PasienModel BabyPasien(
        string pasienId = "BABY01",
        DateOnly? tglLahir = null,
        string mrIbu = "MOM01")
    {
        var birthDate = tglLahir ?? DateOnly.FromDateTime(TestTglJamProvider.Instance.Now).AddDays(-5);
        var person = new PersonInfoType(
            "Bayi Ny. Ibu", birthDate, "L",
            AlamatType.Default, ContactType.Default, IdentitasType.Default);
        return new PasienModel(
            pasienId, person, "Bayi", "Pekalongan",
            GolDarahType.Default, "-", KtpType.Default,
            KelurahanType.Default, IdentitasType.Default,
            [], PasienKeluargaType.Default,
            AgamaType.Default, SukuType.Default, StatusKawinDkType.Default,
            PendidikanDkType.Default, PekerjaanDkType.Default,
            DateTime.MinValue, false, mrIbu: mrIbu);
    }

    private static PasienModel AdultPasien(
        string pasienId = "ADULT01",
        DateOnly? tglLahir = null)
    {
        var birthDate = tglLahir ?? new DateOnly(1990, 5, 20);
        var person = new PersonInfoType(
            "Pasien Dewasa", birthDate, "L",
            AlamatType.Default, ContactType.Default, IdentitasType.Default);
        return new PasienModel(
            pasienId, person, "Dewasa", "Pekalongan",
            GolDarahType.Default, "-", KtpType.Default,
            KelurahanType.Default, IdentitasType.Default,
            [], PasienKeluargaType.Default,
            AgamaType.Default, SukuType.Default, StatusKawinDkType.Default,
            PendidikanDkType.Default, PekerjaanDkType.Default,
            DateTime.MinValue, false);
    }

    private static RegDto MotherRegDto(
        string regId = "REG-IBU-01",
        string mrIbu = "MOM01",
        string jenisInap = "2",
        string tglKeluar = "3000-01-01",
        string tglVoid = "3000-01-01") =>
        new(
            regId, "2026-10-25", "08:00:00", "user1",
            tglKeluar, "00:00:00", "-",
            "3000-01-01", "00:00:00", "-",
            tglVoid, "00:00:00", "-",
            "1", mrIbu, "00000", "1",
            "1", "-", "D01",
            "RI1", "K01", "-", "-",
            "-", "2026-10-25 08:00:00", "3000-01-01 00:00:00", 0,
            jenisInap, "",
            "Ibu Pasien", "1995-05-10", "P",
            "Umum", "Kelas 1", "Datang Sendiri",
            "-", "Dr. Medis", "Rawat Inap", "Karcis");

    [Fact]
    public async Task GivenPerinatalPatient_WithValidExplicitMotherReg_WhenProcessed_ThenSetsPerinatalAndMotherReg()
    {
        var harness = CreateOpnameHarness();
        var baby = BabyPasien();
        harness.PasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(baby));
        harness.RegDal.Setup(x => x.GetData(It.Is<IRegKey>(k => k.RegId == "REG-IBU-01")))
            .Returns(MotherRegDto("REG-IBU-01", jenisInap: "2"));

        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2",
            RegistrationData() with { JenisInapId = "1", RegIbuId = "REG-IBU-01" });

        var response = await harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        response.RegId.Should().NotBeNullOrWhiteSpace();
        harness.SavedReg.Should().NotBeNull();
        harness.SavedReg!.JenisInap.JenisInapId.Should().Be("3");
        harness.SavedReg.RegIbu.Should().Be("REG-IBU-01");
        harness.RegDal.Verify(x => x.GetData(It.Is<IRegKey>(k => k.RegId == "REG-IBU-01")), Times.Once);
    }

    [Fact]
    public async Task GivenPerinatalPatient_WithoutPayloadMotherReg_WhenActiveMotherRegResolvedFromMrIbu_ThenSetsPerinatalAndMotherReg()
    {
        var harness = CreateOpnameHarness();
        var baby = BabyPasien(mrIbu: "MOM-AUTO");
        harness.PasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(baby));
        var motherReg = MotherRegDto("REG-AUTO-99", "MOM-AUTO", jenisInap: "2");
        harness.RegDal.Setup(x => x.ListData(It.Is<IPasienKey>(k => k.PasienId == "MOM-AUTO")))
            .Returns([motherReg]);
        harness.RegDal.Setup(x => x.GetData(It.Is<IRegKey>(k => k.RegId == "REG-AUTO-99")))
            .Returns(motherReg);

        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2",
            RegistrationData() with { JenisInapId = null, RegIbuId = null });

        var response = await harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        response.RegId.Should().NotBeNullOrWhiteSpace();
        harness.SavedReg.Should().NotBeNull();
        harness.SavedReg!.JenisInap.JenisInapId.Should().Be("3");
        harness.SavedReg.RegIbu.Should().Be("REG-AUTO-99");
        harness.RegDal.Verify(x => x.ListData(It.Is<IPasienKey>(k => k.PasienId == "MOM-AUTO")), Times.Once);
        harness.RegDal.Verify(x => x.GetData(It.Is<IRegKey>(k => k.RegId == "REG-AUTO-99")), Times.Once);
    }

    [Fact]
    public async Task GivenPerinatalPatient_WhenNoMotherRegInPayloadAndNoActiveMotherRegFound_ThenThrowsInvalidOperationException()
    {
        var harness = CreateOpnameHarness();
        var baby = BabyPasien(mrIbu: "");
        harness.PasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(baby));

        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2",
            RegistrationData() with { RegIbuId = null });

        var act = () => harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Pasien perinatal wajib menyertakan Register Ibu yang valid.");
        harness.AdmissionRepo.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Never);
        harness.RegRepo.Verify(x => x.SaveChanges(It.IsAny<RegModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenPerinatalPatient_WhenMotherRegNotFoundInDatabase_ThenThrowsInvalidOperationException()
    {
        var harness = CreateOpnameHarness();
        var baby = BabyPasien();
        harness.PasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(baby));
        harness.RegDal.Setup(x => x.GetData(It.Is<IRegKey>(k => k.RegId == "NOT-FOUND")))
            .Returns((RegDto?)null);

        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2",
            RegistrationData() with { RegIbuId = "NOT-FOUND" });

        var act = () => harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Register Ibu 'NOT-FOUND' tidak ditemukan.");
        harness.AdmissionRepo.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenPerinatalPatient_WhenMotherRegIsVoided_ThenThrowsInvalidOperationException()
    {
        var harness = CreateOpnameHarness();
        var baby = BabyPasien();
        harness.PasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(baby));
        harness.RegDal.Setup(x => x.GetData(It.Is<IRegKey>(k => k.RegId == "REG-VOID")))
            .Returns(MotherRegDto("REG-VOID", tglVoid: "2026-10-26"));

        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2",
            RegistrationData() with { RegIbuId = "REG-VOID" });

        var act = () => harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Register Ibu 'REG-VOID' sudah dibatalkan (void).");
        harness.AdmissionRepo.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenPerinatalPatient_WhenMotherRegIsAlreadyDischarged_ThenThrowsInvalidOperationException()
    {
        var harness = CreateOpnameHarness();
        var baby = BabyPasien();
        harness.PasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(baby));
        harness.RegDal.Setup(x => x.GetData(It.Is<IRegKey>(k => k.RegId == "REG-PULANG")))
            .Returns(MotherRegDto("REG-PULANG", tglKeluar: "2026-10-26"));

        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2",
            RegistrationData() with { RegIbuId = "REG-PULANG" });

        var act = () => harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Register Ibu 'REG-PULANG' sudah keluar/pulang.");
        harness.AdmissionRepo.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenPerinatalPatient_WhenMotherRegIsNotObstetri_ThenThrowsSpecificJenisInapIbuInvalidException()
    {
        var harness = CreateOpnameHarness();
        var baby = BabyPasien();
        harness.PasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(baby));
        harness.RegDal.Setup(x => x.GetData(It.Is<IRegKey>(k => k.RegId == "REG-UMUM")))
            .Returns(MotherRegDto("REG-UMUM", jenisInap: "1"));

        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2",
            RegistrationData() with { RegIbuId = "REG-UMUM" });

        var act = () => harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("JENIS INAP IBU invalid");
        harness.AdmissionRepo.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Never);
        harness.RegRepo.Verify(x => x.SaveChanges(It.IsAny<RegModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenAdultPatient_WhenObstetriRequested_ThenPersistsObstetriAndEmptyMotherReg()
    {
        var harness = CreateOpnameHarness();
        var adult = AdultPasien();
        harness.PasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(adult));

        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2",
            RegistrationData() with { JenisInapId = "2", RegIbuId = "SHOULD_BE_IGNORED" });

        var response = await harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        response.RegId.Should().NotBeNullOrWhiteSpace();
        harness.SavedReg.Should().NotBeNull();
        harness.SavedReg!.JenisInap.JenisInapId.Should().Be("2");
        harness.SavedReg.RegIbu.Should().BeEmpty();
        harness.RegDal.Verify(x => x.GetData(It.IsAny<IRegKey>()), Times.Never);
    }

    [Fact]
    public async Task GivenAdultPatient_WhenNoJenisInapRequested_ThenDefaultsToUmumAndEmptyMotherReg()
    {
        var harness = CreateOpnameHarness();
        var adult = AdultPasien();
        harness.PasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(adult));

        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2",
            RegistrationData() with { JenisInapId = null, RegIbuId = null });

        var response = await harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        response.RegId.Should().NotBeNullOrWhiteSpace();
        harness.SavedReg.Should().NotBeNull();
        harness.SavedReg!.JenisInap.JenisInapId.Should().Be("1");
        harness.SavedReg.RegIbu.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenAdultPatient_WhenPerinatalRequestedInPayload_ThenForcesUmumAndEmptyMotherReg()
    {
        var harness = CreateOpnameHarness();
        var adult = AdultPasien();
        harness.PasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(adult));

        var command = new AdmProcessOpnameRequestCmd(
            harness.Opname.OpnameRequestId, "1", "B1", "user2",
            RegistrationData() with { JenisInapId = "3", RegIbuId = "MOM-IGNORED" });

        var response = await harness.Sut.ProcessOpnameRequest(command, CancellationToken.None);

        response.RegId.Should().NotBeNullOrWhiteSpace();
        harness.SavedReg.Should().NotBeNull();
        harness.SavedReg!.JenisInap.JenisInapId.Should().Be("1");
        harness.SavedReg.RegIbu.Should().BeEmpty();
        harness.RegDal.Verify(x => x.GetData(It.IsAny<IRegKey>()), Times.Never);
    }

    [Fact]
    public async Task GivenPerinatalPatient_InReservationFlow_WithValidMotherReg_ThenSetsPerinatalAndMotherReg()
    {
        var harness = CreateReservationHarness();
        var baby = BabyPasien();
        harness.PasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(baby));
        harness.RegDal.Setup(x => x.GetData(It.Is<IRegKey>(k => k.RegId == "REG-IBU-RES")))
            .Returns(MotherRegDto("REG-IBU-RES", jenisInap: "2"));

        var command = new AdmProcessReservationCmd(
            harness.Reservation.ReservationId, "1", "B1", "user2",
            RegistrationData() with { RegIbuId = "REG-IBU-RES" });

        var response = await harness.Sut.ProcessReservation(command, CancellationToken.None);

        response.RegId.Should().NotBeNullOrWhiteSpace();
        harness.SavedRegInap.Should().NotBeNull();
        harness.AdmissionRepo.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Once);
        harness.RegDal.Verify(x => x.GetData(It.Is<IRegKey>(k => k.RegId == "REG-IBU-RES")), Times.Once);
    }

    #endregion

    private static OpnameHarness CreateOpnameHarness(
        IAdmisiEventPublisher? customPublisher = null,
        Mock<IPasienTrackerInpatientCoordinator>? customTrackerCoordinator = null)
    {
        var opname = OpnameRequestModel.Create(PasienModel.Default.ToReff(),
            PpaType.Default.ToReff(), DateTime.Today.AddDays(1), "test", "user1", "CH0001");

        var admissionRepo = new Mock<IAdmissionRepo>();
        var opnameRepo = new Mock<IOpnameRequestRepo>();
        var reservationRepo = new Mock<IReservationRepo>();
        var regRepo = new Mock<IRegRepo>();
        var regInapRepo = new Mock<IRegInapRepo>();
        var regAktifRepo = new Mock<IRegAktifRepo>();
        var prosedurRepo = new Mock<IProsedureMasukInapRepo>();
        var auditRepo = new Mock<IAuditRepo>();
        opnameRepo.Setup(x => x.LoadEntity(It.IsAny<IOpnameRequestKey>())).Returns(MayBe.From(opname));
        admissionRepo.Setup(x => x.ListData(It.IsAny<AdmissionListFilter>())).Returns([]);
        regAktifRepo.Setup(x => x.IsPasienAktif(It.IsAny<IPasienKey>())).Returns(false);
        prosedurRepo.Setup(x => x.LoadEntity(It.IsAny<IProsedurMasukInapKey>()))
            .Returns(MayBe.From(new ProsedurMasukInapType("IGD", "Dari IGD", ProsedurMasukInapDkType.Default)));

        var pasienRepo = new Mock<IPasienRepo>();
        pasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(PasienModel.Default));
        var tipeJaminanRepo = new Mock<ITipeJaminanRepo>();
        tipeJaminanRepo.Setup(x => x.LoadEntity(It.IsAny<ITipeJaminanKey>()))
            .Returns(MayBe.From(TipeJaminanType.BayarSendiri));
        var caraMasukRepo = new Mock<ICaraMasukDkRepo>();
        caraMasukRepo.Setup(x => x.LoadEntity(It.IsAny<ICaraMasukDkKey>()))
            .Returns(MayBe.From(CaraMasukDkType.DatangSendiri));
        var rujukanRepo = new Mock<IRujukanRepo>();
        rujukanRepo.Setup(x => x.LoadEntity(It.IsAny<IRujukanKey>()))
            .Returns(MayBe.From(RujukanType.Default));
        var ppaRepo = new Mock<IPpaRepo>();
        ppaRepo.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(PpaType.Default));
        var layanan = InpatientLayanan();
        var layananRepo = new Mock<ILayananRepo>();
        layananRepo.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>())).Returns(MayBe.From(layanan));
        var bangsalRepo = new Mock<IBangsalRepo>();
        bangsalRepo.Setup(x => x.LoadEntity(It.IsAny<IBangsalKey>()))
            .Returns(MayBe.From(new BangsalType("B1", "Bangsal 1", RoomCatType.Default, layanan.ToReff())));

        var ward = new Mock<IWardAccommodationGateway>();
        ward.Setup(x => x.ResolveKelasDk("1")).Returns(new KelasDkType("1", "Kelas 1"));
        ward.Setup(x => x.ResolveBangsalForCareClass("B1", "1"))
            .Returns(new BangsalReff("B1", "Bangsal 1"));
        ward.Setup(x => x.ResolveKelas("K1")).Returns(new KelasReff("K1", "Kelas 1"));
        ward.Setup(x => x.ResolveKelas("VIP")).Returns(new KelasReff("VIP", "Kelas VIP"));
        ward.Setup(x => x.ResolveKelasForCareClass("B1", "1")).Returns(new KelasReff("K1", "Kelas 1"));
        ward.Setup(x => x.ResolveBangsal("B1")).Returns(new BangsalReff("B1", "Bangsal 1"));

        var waitingListRepo = new Mock<IWaitingListRepo>();
        WaitingListModel? savedWaitingList = null;
        waitingListRepo.Setup(x => x.SaveChanges(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(x => savedWaitingList = x);

        var factory = new RegFactory(Mock.Of<ISequencerManual>(),
            Mock.Of<IGetKelasRajalService>(), Mock.Of<IGetKelasRadarService>());

        AdmissionModel? savedAdmission = null;
        RegModel? savedReg = null;
        RegInapModel? savedRegInap = null;
        RegAktifModel? savedRegAktif = null;
        OpnameRequestModel? savedOpname = null;
        admissionRepo.Setup(x => x.SaveChanges(It.IsAny<AdmissionModel>()))
            .Callback<AdmissionModel>(x => savedAdmission = x);
        regRepo.Setup(x => x.SaveChanges(It.IsAny<RegModel>()))
            .Callback<RegModel>(x => savedReg = x);
        regInapRepo.Setup(x => x.SaveChanges(It.IsAny<RegInapModel>()))
            .Callback<RegInapModel>(x => savedRegInap = x);
        regAktifRepo.Setup(x => x.SaveChanges(It.IsAny<RegAktifModel>()))
            .Callback<RegAktifModel>(x => savedRegAktif = x);
        opnameRepo.Setup(x => x.SaveChanges(It.IsAny<OpnameRequestModel>()))
            .Callback<OpnameRequestModel>(x => savedOpname = x);

        var regDal = new Mock<IRegDal>();
        var publisher = new Mock<IAdmisiEventPublisher>();
        var effectivePublisher = customPublisher ?? publisher.Object;
        var trackerCoordinator = customTrackerCoordinator ?? new Mock<IPasienTrackerInpatientCoordinator>();
        var sut = new AdmissionRegistrationOrchestrator(
            admissionRepo.Object, opnameRepo.Object, reservationRepo.Object, ward.Object,
            bangsalRepo.Object, pasienRepo.Object, tipeJaminanRepo.Object, Mock.Of<IPolisRepo>(),
            caraMasukRepo.Object, rujukanRepo.Object, ppaRepo.Object, layananRepo.Object,
            prosedurRepo.Object, factory, regRepo.Object, regInapRepo.Object,
            regAktifRepo.Object, auditRepo.Object, TestTglJamProvider.Instance,
            effectivePublisher, waitingListRepo.Object, trackerCoordinator.Object,
            regDal.Object);

        return new OpnameHarness(
            sut, opname, publisher, admissionRepo, opnameRepo, regRepo, regInapRepo, regAktifRepo, prosedurRepo,
            auditRepo, caraMasukRepo, rujukanRepo, bangsalRepo, layananRepo, ward, waitingListRepo, trackerCoordinator,
            pasienRepo, regDal,
            () => savedAdmission, () => savedReg, () => savedRegInap, () => savedRegAktif, () => savedOpname,
            () => savedWaitingList);
    }

    private static ReservationHarness CreateReservationHarness(
        IAdmisiEventPublisher? customPublisher = null,
        Mock<IPasienTrackerInpatientCoordinator>? customTrackerCoordinator = null)
    {
        var reservation = ReservationModel.Create(
            PasienModel.Default.ToReff(),
            DateTime.Today.AddDays(2),
            new KelasReff("K1", "Kelas 1"),
            new BangsalReff("B1", "Bangsal 1"),
            "user1");
        reservation = reservation.Maintain(
            reservation.PlannedDate, reservation.KelasRawat, reservation.Bangsal, "user1");

        var admissionRepo = new Mock<IAdmissionRepo>();
        var opnameRepo = new Mock<IOpnameRequestRepo>();
        var reservationRepo = new Mock<IReservationRepo>();
        var regRepo = new Mock<IRegRepo>();
        var regInapRepo = new Mock<IRegInapRepo>();
        var regAktifRepo = new Mock<IRegAktifRepo>();
        var prosedurRepo = new Mock<IProsedureMasukInapRepo>();
        reservationRepo.Setup(x => x.LoadEntity(It.IsAny<IReservationKey>()))
            .Returns(MayBe.From(reservation));
        admissionRepo.Setup(x => x.ListData(It.IsAny<AdmissionListFilter>())).Returns([]);
        regAktifRepo.Setup(x => x.IsPasienAktif(It.IsAny<IPasienKey>())).Returns(false);
        prosedurRepo.Setup(x => x.LoadEntity(It.IsAny<IProsedurMasukInapKey>()))
            .Returns(MayBe.From(new ProsedurMasukInapType("IGD", "Dari IGD", ProsedurMasukInapDkType.Default)));

        var pasienRepo = new Mock<IPasienRepo>();
        pasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(PasienModel.Default));
        var tipeJaminanRepo = new Mock<ITipeJaminanRepo>();
        tipeJaminanRepo.Setup(x => x.LoadEntity(It.IsAny<ITipeJaminanKey>()))
            .Returns(MayBe.From(TipeJaminanType.BayarSendiri));
        var caraMasukRepo = new Mock<ICaraMasukDkRepo>();
        caraMasukRepo.Setup(x => x.LoadEntity(It.IsAny<ICaraMasukDkKey>()))
            .Returns(MayBe.From(CaraMasukDkType.DatangSendiri));
        var rujukanRepo = new Mock<IRujukanRepo>();
        rujukanRepo.Setup(x => x.LoadEntity(It.IsAny<IRujukanKey>()))
            .Returns(MayBe.From(RujukanType.Default));
        var ppaRepo = new Mock<IPpaRepo>();
        ppaRepo.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(PpaType.Default));
        var layanan = InpatientLayanan();
        var layananRepo = new Mock<ILayananRepo>();
        layananRepo.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>())).Returns(MayBe.From(layanan));
        var bangsalRepo = new Mock<IBangsalRepo>();
        bangsalRepo.Setup(x => x.LoadEntity(It.IsAny<IBangsalKey>()))
            .Returns(MayBe.From(new BangsalType("B1", "Bangsal 1", RoomCatType.Default, layanan.ToReff())));

        var ward = new Mock<IWardAccommodationGateway>();
        ward.Setup(x => x.ResolveKelasDk("1")).Returns(new KelasDkType("1", "Kelas 1"));
        ward.Setup(x => x.ResolveBangsalForCareClass("B1", "1"))
            .Returns(new BangsalReff("B1", "Bangsal 1"));
        ward.Setup(x => x.ResolveKelas("K1")).Returns(new KelasReff("K1", "Kelas 1"));
        ward.Setup(x => x.ResolveKelas("VIP")).Returns(new KelasReff("VIP", "Kelas VIP"));
        ward.Setup(x => x.ResolveKelasForCareClass("B1", "1")).Returns(new KelasReff("K1", "Kelas 1"));
        ward.Setup(x => x.ResolveBangsal("B1")).Returns(new BangsalReff("B1", "Bangsal 1"));

        var waitingListRepo = new Mock<IWaitingListRepo>();
        WaitingListModel? savedWaitingList = null;
        waitingListRepo.Setup(x => x.SaveChanges(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(x => savedWaitingList = x);

        var auditRepo = new Mock<IAuditRepo>();
        var factory = new RegFactory(Mock.Of<ISequencerManual>(),
            Mock.Of<IGetKelasRajalService>(), Mock.Of<IGetKelasRadarService>());

        RegInapModel? savedRegInap = null;
        reservationRepo.Setup(x => x.SaveChanges(It.IsAny<ReservationModel>()));
        admissionRepo.Setup(x => x.SaveChanges(It.IsAny<AdmissionModel>()));
        regRepo.Setup(x => x.SaveChanges(It.IsAny<RegModel>()));
        regInapRepo.Setup(x => x.SaveChanges(It.IsAny<RegInapModel>()))
            .Callback<RegInapModel>(x => savedRegInap = x);
        regAktifRepo.Setup(x => x.SaveChanges(It.IsAny<RegAktifModel>()));

        var regDal = new Mock<IRegDal>();
        var publisher = new Mock<IAdmisiEventPublisher>();
        var effectivePublisher = customPublisher ?? publisher.Object;
        var trackerCoordinator = customTrackerCoordinator ?? new Mock<IPasienTrackerInpatientCoordinator>();
        var sut = new AdmissionRegistrationOrchestrator(
            admissionRepo.Object, opnameRepo.Object, reservationRepo.Object, ward.Object,
            bangsalRepo.Object, pasienRepo.Object, tipeJaminanRepo.Object, Mock.Of<IPolisRepo>(),
            caraMasukRepo.Object, rujukanRepo.Object, ppaRepo.Object, layananRepo.Object,
            prosedurRepo.Object, factory, regRepo.Object, regInapRepo.Object,
            regAktifRepo.Object, auditRepo.Object, TestTglJamProvider.Instance,
            effectivePublisher, waitingListRepo.Object, trackerCoordinator.Object,
            regDal.Object);

        return new ReservationHarness(
            sut, reservation, publisher, admissionRepo, prosedurRepo, regInapRepo, auditRepo,
            ward, waitingListRepo, trackerCoordinator, pasienRepo, regDal, () => savedRegInap, () => savedWaitingList);
    }

    private static LayananType InpatientLayanan() =>
        LayananType.Default with
        {
            LayananId = "RI1", LayananName = "Rawat Inap", InstalasiDk = InstalasiDkType.RawatInap
        };

    private sealed class OpnameHarness(
        AdmissionRegistrationOrchestrator sut,
        OpnameRequestModel opname,
        Mock<IAdmisiEventPublisher> publisher,
        Mock<IAdmissionRepo> admissionRepo,
        Mock<IOpnameRequestRepo> opnameRepo,
        Mock<IRegRepo> regRepo,
        Mock<IRegInapRepo> regInapRepo,
        Mock<IRegAktifRepo> regAktifRepo,
        Mock<IProsedureMasukInapRepo> prosedurRepo,
        Mock<IAuditRepo> auditRepo,
        Mock<ICaraMasukDkRepo> caraMasukRepo,
        Mock<IRujukanRepo> rujukanRepo,
        Mock<IBangsalRepo> bangsalRepo,
        Mock<ILayananRepo> layananRepo,
        Mock<IWardAccommodationGateway> wardGateway,
        Mock<IWaitingListRepo> waitingListRepo,
        Mock<IPasienTrackerInpatientCoordinator> trackerCoordinator,
        Mock<IPasienRepo> pasienRepo,
        Mock<IRegDal> regDal,
        Func<AdmissionModel?> savedAdmission,
        Func<RegModel?> savedReg,
        Func<RegInapModel?> savedRegInap,
        Func<RegAktifModel?> savedRegAktif,
        Func<OpnameRequestModel?> savedOpname,
        Func<WaitingListModel?> savedWaitingList)
    {
        public AdmissionRegistrationOrchestrator Sut { get; } = sut;
        public OpnameRequestModel Opname { get; } = opname;
        public Mock<IAdmisiEventPublisher> Publisher { get; } = publisher;
        public Mock<IAdmissionRepo> AdmissionRepo { get; } = admissionRepo;
        public Mock<IOpnameRequestRepo> OpnameRepo { get; } = opnameRepo;
        public Mock<IRegRepo> RegRepo { get; } = regRepo;
        public Mock<IRegInapRepo> RegInapRepo { get; } = regInapRepo;
        public Mock<IRegAktifRepo> RegAktifRepo { get; } = regAktifRepo;
        public Mock<IProsedureMasukInapRepo> ProsedurRepo { get; } = prosedurRepo;
        public Mock<IAuditRepo> AuditRepo { get; } = auditRepo;
        public Mock<ICaraMasukDkRepo> CaraMasukRepo { get; } = caraMasukRepo;
        public Mock<IRujukanRepo> RujukanRepo { get; } = rujukanRepo;
        public Mock<IBangsalRepo> BangsalRepo { get; } = bangsalRepo;
        public Mock<ILayananRepo> LayananRepo { get; } = layananRepo;
        public Mock<IWardAccommodationGateway> WardGateway { get; } = wardGateway;
        public Mock<IWaitingListRepo> WaitingListRepo { get; } = waitingListRepo;
        public Mock<IPasienTrackerInpatientCoordinator> TrackerCoordinator { get; } = trackerCoordinator;
        public Mock<IPasienRepo> PasienRepo { get; } = pasienRepo;
        public Mock<IRegDal> RegDal { get; } = regDal;
        public AdmissionModel? SavedAdmission => savedAdmission();
        public RegModel? SavedReg => savedReg();
        public RegInapModel? SavedRegInap => savedRegInap();
        public RegAktifModel? SavedRegAktif => savedRegAktif();
        public OpnameRequestModel? SavedOpname => savedOpname();
        public WaitingListModel? SavedWaitingList => savedWaitingList();
    }

    private sealed class ReservationHarness(
        AdmissionRegistrationOrchestrator sut,
        ReservationModel reservation,
        Mock<IAdmisiEventPublisher> publisher,
        Mock<IAdmissionRepo> admissionRepo,
        Mock<IProsedureMasukInapRepo> prosedurRepo,
        Mock<IRegInapRepo> regInapRepo,
        Mock<IAuditRepo> auditRepo,
        Mock<IWardAccommodationGateway> wardGateway,
        Mock<IWaitingListRepo> waitingListRepo,
        Mock<IPasienTrackerInpatientCoordinator> trackerCoordinator,
        Mock<IPasienRepo> pasienRepo,
        Mock<IRegDal> regDal,
        Func<RegInapModel?> savedRegInap,
        Func<WaitingListModel?> savedWaitingList)
    {
        public AdmissionRegistrationOrchestrator Sut { get; } = sut;
        public ReservationModel Reservation { get; } = reservation;
        public Mock<IAdmisiEventPublisher> Publisher { get; } = publisher;
        public Mock<IAdmissionRepo> AdmissionRepo { get; } = admissionRepo;
        public Mock<IProsedureMasukInapRepo> ProsedurRepo { get; } = prosedurRepo;
        public Mock<IRegInapRepo> RegInapRepo { get; } = regInapRepo;
        public Mock<IAuditRepo> AuditRepo { get; } = auditRepo;
        public Mock<IWardAccommodationGateway> WardGateway { get; } = wardGateway;
        public Mock<IWaitingListRepo> WaitingListRepo { get; } = waitingListRepo;
        public Mock<IPasienTrackerInpatientCoordinator> TrackerCoordinator { get; } = trackerCoordinator;
        public Mock<IPasienRepo> PasienRepo { get; } = pasienRepo;
        public Mock<IRegDal> RegDal { get; } = regDal;
        public RegInapModel? SavedRegInap => savedRegInap();
        public WaitingListModel? SavedWaitingList => savedWaitingList();
    }
}
