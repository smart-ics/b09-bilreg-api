using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Application.AdmisiRanapContext.ReservationFeature;
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
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.Shared.Helpers;
using FluentAssertions;
using Moq;
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
        harness.SavedRegInap!.RegId.Should().Be(response.RegId);
        harness.SavedRegInap.ProsedurMasukInap.ProsedurMasukInapId.Should().Be("IGD");
        harness.SavedRegInap.Dpjp.PpaId.Should().Be(harness.SavedReg.Dokter.PpaId);
        harness.SavedRegAktif!.RegId.Should().Be(response.RegId);
        harness.SavedOpname!.FulfilledRegId.Should().Be(response.RegId);
        harness.ProsedurRepo.Verify(
            x => x.LoadEntity(It.Is<IProsedurMasukInapKey>(k => k.ProsedurMasukInapId == "IGD")),
            Times.Once);
        harness.RegInapRepo.Verify(x => x.SaveChanges(It.IsAny<RegInapModel>()), Times.Once);
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
    }

    private static AdmissionRegistrationData RegistrationData() =>
        new("00000", "1", "IGD", "-", "-", "RI1", "KRI", "PESERTA1");

    private static OpnameHarness CreateOpnameHarness()
    {
        var opname = OpnameRequestModel.Create(PasienModel.Default.ToReff(),
            PpaType.Default.ToReff(), DateTime.Today.AddDays(1), "test", "user1");
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
        var (layanan, karcis) = InpatientVisit();
        var layananRepo = new Mock<ILayananRepo>();
        layananRepo.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>())).Returns(MayBe.From(layanan));
        var karcisRepo = new Mock<IKarcisRepo>();
        karcisRepo.Setup(x => x.LoadEntity(It.IsAny<IKarcisKey>())).Returns(MayBe.From(karcis));

        var ward = new Mock<IWardAccommodationGateway>();
        ward.Setup(x => x.ResolveKelasDk("1")).Returns(new KelasDkType("1", "Kelas 1"));
        ward.Setup(x => x.ResolveBangsalForCareClass("B1", "1"))
            .Returns(new BangsalReff("B1", "Bangsal 1"));
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

        var sut = new AdmissionRegistrationOrchestrator(
            admissionRepo.Object, opnameRepo.Object, reservationRepo.Object, ward.Object,
            pasienRepo.Object, tipeJaminanRepo.Object, Mock.Of<IPolisRepo>(),
            caraMasukRepo.Object, rujukanRepo.Object, ppaRepo.Object, layananRepo.Object,
            karcisRepo.Object, prosedurRepo.Object, factory, regRepo.Object, regInapRepo.Object,
            regAktifRepo.Object, auditRepo.Object);

        return new OpnameHarness(
            sut, opname, admissionRepo, opnameRepo, regRepo, regInapRepo, regAktifRepo, prosedurRepo,
            auditRepo,
            () => savedAdmission, () => savedReg, () => savedRegInap, () => savedRegAktif, () => savedOpname);
    }

    private static ReservationHarness CreateReservationHarness()
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
        var (layanan, karcis) = InpatientVisit();
        var layananRepo = new Mock<ILayananRepo>();
        layananRepo.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>())).Returns(MayBe.From(layanan));
        var karcisRepo = new Mock<IKarcisRepo>();
        karcisRepo.Setup(x => x.LoadEntity(It.IsAny<IKarcisKey>())).Returns(MayBe.From(karcis));

        var ward = new Mock<IWardAccommodationGateway>();
        ward.Setup(x => x.ResolveKelasDk("1")).Returns(new KelasDkType("1", "Kelas 1"));
        ward.Setup(x => x.ResolveBangsalForCareClass("B1", "1"))
            .Returns(new BangsalReff("B1", "Bangsal 1"));
        var factory = new RegFactory(Mock.Of<ISequencerManual>(),
            Mock.Of<IGetKelasRajalService>(), Mock.Of<IGetKelasRadarService>());

        RegInapModel? savedRegInap = null;
        reservationRepo.Setup(x => x.SaveChanges(It.IsAny<ReservationModel>()));
        admissionRepo.Setup(x => x.SaveChanges(It.IsAny<AdmissionModel>()));
        regRepo.Setup(x => x.SaveChanges(It.IsAny<RegModel>()));
        regInapRepo.Setup(x => x.SaveChanges(It.IsAny<RegInapModel>()))
            .Callback<RegInapModel>(x => savedRegInap = x);
        regAktifRepo.Setup(x => x.SaveChanges(It.IsAny<RegAktifModel>()));

        var sut = new AdmissionRegistrationOrchestrator(
            admissionRepo.Object, opnameRepo.Object, reservationRepo.Object, ward.Object,
            pasienRepo.Object, tipeJaminanRepo.Object, Mock.Of<IPolisRepo>(),
            caraMasukRepo.Object, rujukanRepo.Object, ppaRepo.Object, layananRepo.Object,
            karcisRepo.Object, prosedurRepo.Object, factory, regRepo.Object, regInapRepo.Object,
            regAktifRepo.Object, Mock.Of<IAuditRepo>());

        return new ReservationHarness(
            sut, reservation, admissionRepo, prosedurRepo, regInapRepo, () => savedRegInap);
    }

    private static (LayananType, KarcisType) InpatientVisit()
    {
        var layanan = LayananType.Default with
        {
            LayananId = "RI1", LayananName = "Rawat Inap", InstalasiDk = InstalasiDkType.RawatInap
        };
        var karcis = new KarcisType("KRI", "Karcis Inap", true,
            InstalasiDkType.RawatInap, RekapCetakType.Default.ToReff(),
            TarifType.Default.ToReff(), [], [layanan.ToReff()]);
        return (layanan, karcis);
    }

    private sealed class OpnameHarness(
        AdmissionRegistrationOrchestrator sut,
        OpnameRequestModel opname,
        Mock<IAdmissionRepo> admissionRepo,
        Mock<IOpnameRequestRepo> opnameRepo,
        Mock<IRegRepo> regRepo,
        Mock<IRegInapRepo> regInapRepo,
        Mock<IRegAktifRepo> regAktifRepo,
        Mock<IProsedureMasukInapRepo> prosedurRepo,
        Mock<IAuditRepo> auditRepo,
        Func<AdmissionModel?> savedAdmission,
        Func<RegModel?> savedReg,
        Func<RegInapModel?> savedRegInap,
        Func<RegAktifModel?> savedRegAktif,
        Func<OpnameRequestModel?> savedOpname)
    {
        public AdmissionRegistrationOrchestrator Sut { get; } = sut;
        public OpnameRequestModel Opname { get; } = opname;
        public Mock<IAdmissionRepo> AdmissionRepo { get; } = admissionRepo;
        public Mock<IOpnameRequestRepo> OpnameRepo { get; } = opnameRepo;
        public Mock<IRegRepo> RegRepo { get; } = regRepo;
        public Mock<IRegInapRepo> RegInapRepo { get; } = regInapRepo;
        public Mock<IRegAktifRepo> RegAktifRepo { get; } = regAktifRepo;
        public Mock<IProsedureMasukInapRepo> ProsedurRepo { get; } = prosedurRepo;
        public Mock<IAuditRepo> AuditRepo { get; } = auditRepo;
        public AdmissionModel? SavedAdmission => savedAdmission();
        public RegModel? SavedReg => savedReg();
        public RegInapModel? SavedRegInap => savedRegInap();
        public RegAktifModel? SavedRegAktif => savedRegAktif();
        public OpnameRequestModel? SavedOpname => savedOpname();
    }

    private sealed class ReservationHarness(
        AdmissionRegistrationOrchestrator sut,
        ReservationModel reservation,
        Mock<IAdmissionRepo> admissionRepo,
        Mock<IProsedureMasukInapRepo> prosedurRepo,
        Mock<IRegInapRepo> regInapRepo,
        Func<RegInapModel?> savedRegInap)
    {
        public AdmissionRegistrationOrchestrator Sut { get; } = sut;
        public ReservationModel Reservation { get; } = reservation;
        public Mock<IAdmissionRepo> AdmissionRepo { get; } = admissionRepo;
        public Mock<IProsedureMasukInapRepo> ProsedurRepo { get; } = prosedurRepo;
        public Mock<IRegInapRepo> RegInapRepo { get; } = regInapRepo;
        public RegInapModel? SavedRegInap => savedRegInap();
    }
}
