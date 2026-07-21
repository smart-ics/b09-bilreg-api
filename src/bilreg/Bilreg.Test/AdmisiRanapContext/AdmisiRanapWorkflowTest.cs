using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature.UseCases;
using Bilreg.Application.AdmisiRanapContext.ReservationFeature;
using Bilreg.Application.AdmisiRanapContext.ReservationFeature.UseCases;
using Bilreg.Application.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Application.AdmisiRanapContext.WaitingListFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext;

/// <summary>
/// End-to-end workflow tests chaining real handlers with in-memory repo state.
/// </summary>
public class AdmisiRanapWorkflowTest
{
    [Fact]
    public async Task WF01_DirectAdmission_WithWaitingList_CompletesChain()
    {
        var h = new WorkflowHarness();

        var opnameResponse = await h.CreateOpnameHandler.Handle(
            new AdmCreateOpnameRequestCmd("P001", "D001","2026-07-30", "Direct", "user1"),
            CancellationToken.None);

        var admissionResponse = await h.ProcessOpnameHandler.Handle(
            new AdmProcessOpnameRequestCmd(opnameResponse.OpnameRequestId, "1", "B1", "user2", RegistrationData()),
            CancellationToken.None);

        h.Opnames[opnameResponse.OpnameRequestId].OpnameRequestStatus
            .Should().Be(OpnameRequestStatusEnum.Fulfilled);
        h.Admissions[admissionResponse.RegId].AdmissionStatus
            .Should().Be(AdmissionStatusEnum.Admitted);

        var wlResponse = await h.CreateWaitingListHandler.Handle(
            new AdmCreateWaitingListCmd(admissionResponse.RegId, "K1", "B1", 5, "user3"),
            CancellationToken.None);

        h.WaitingLists[wlResponse.WaitingListId].WaitingListStatus
            .Should().Be(WaitingListStatusEnum.Waiting);
    }

    [Fact]
    public async Task WF02_PlannedAdmission_ViaReservation_CompletesChain()
    {
        var h = new WorkflowHarness();

        await h.CreateOpnameHandler.Handle(
            new AdmCreateOpnameRequestCmd("P001", "D001", "2026-07-30", "Planned clinical", "user1"),
            CancellationToken.None);

        var reservationResponse = await h.CreateReservationHandler.Handle(
            new AdmCreateReservationCmd("P001", "2026-09-01", "K1", "B1", "user1"),
            CancellationToken.None);

        await h.MaintainReservationHandler.Handle(
            new AdmMaintainReservationCmd(
                reservationResponse.ReservationId,
                "2026-09-05",
                "K1",
                "B1",
                "user2"),
            CancellationToken.None);

        var admissionResponse = await h.ProcessReservationHandler.Handle(
            new AdmProcessReservationCmd(reservationResponse.ReservationId, "1", "B1", "user3", RegistrationData()),
            CancellationToken.None);

        h.Reservations[reservationResponse.ReservationId].ReservationStatus
            .Should().Be(ReservationStatusEnum.Realized);
        h.Admissions[admissionResponse.RegId].ReservationId
            .Should().Be(reservationResponse.ReservationId);
    }

    [Fact]
    public async Task WF03_ElectiveAdmission_ViaReservation_CompletesChain()
    {
        var h = new WorkflowHarness();

        var reservationResponse = await h.CreateReservationHandler.Handle(
            new AdmCreateReservationCmd("P001", "2026-10-01", "K1", "B1", "user1"),
            CancellationToken.None);

        await h.MaintainReservationHandler.Handle(
            new AdmMaintainReservationCmd(
                reservationResponse.ReservationId,
                "2026-10-01",
                "K2",
                "B2",
                "user2"),
            CancellationToken.None);

        var admissionResponse = await h.ProcessReservationHandler.Handle(
            new AdmProcessReservationCmd(reservationResponse.ReservationId, "2", "B2", "user3", RegistrationData()),
            CancellationToken.None);

        h.Admissions[admissionResponse.RegId].AdmissionStatus
            .Should().Be(AdmissionStatusEnum.Admitted);
        h.Admissions[admissionResponse.RegId].OpnameRequestId.Should().Be("-");
    }

    [Fact]
    public async Task WF04_PatientTransfer_CreateWaitingList_ForExistingAdmission()
    {
        var h = new WorkflowHarness();

        var opnameResponse = await h.CreateOpnameHandler.Handle(
            new AdmCreateOpnameRequestCmd("P001", "D001", "2026-07-30", "Transfer prep", "user1"),
            CancellationToken.None);

        var admissionResponse = await h.ProcessOpnameHandler.Handle(
            new AdmProcessOpnameRequestCmd(opnameResponse.OpnameRequestId, "1", "B1", "user2", RegistrationData()),
            CancellationToken.None);

        var wlResponse = await h.CreateWaitingListHandler.Handle(
            new AdmCreateWaitingListCmd(admissionResponse.RegId, "K2", "B2", 1, "user3"),
            CancellationToken.None);

        var wl = h.WaitingLists[wlResponse.WaitingListId];
        wl.RegId.Should().Be(admissionResponse.RegId);
        wl.WaitingListStatus.Should().Be(WaitingListStatusEnum.Waiting);
        wl.Bangsal.BangsalId.Should().Be("B2");
    }

    private sealed class WorkflowHarness
    {
        public Dictionary<string, OpnameRequestModel> Opnames { get; } = new();
        public Dictionary<string, ReservationModel> Reservations { get; } = new();
        public Dictionary<string, AdmissionModel> Admissions { get; } = new();
        public Dictionary<string, WaitingListModel> WaitingLists { get; } = new();

        public AdmCreateOpnameRequestHandler CreateOpnameHandler { get; }
        public AdmProcessOpnameRequestHandler ProcessOpnameHandler { get; }
        public AdmCreateReservationHandler CreateReservationHandler { get; }
        public AdmMaintainReservationHandler MaintainReservationHandler { get; }
        public AdmProcessReservationHandler ProcessReservationHandler { get; }
        public AdmCreateWaitingListHandler CreateWaitingListHandler { get; }

        public WorkflowHarness()
        {
            var opnameRepo = CreateOpnameRepo();
            var reservationRepo = CreateReservationRepo();
            var admissionRepo = CreateAdmissionRepo();
            var waitingListRepo = CreateWaitingListRepo();
            var patientGateway = CreatePatientGateway();
            var doctorGateway = CreateDoctorGateway();
            var wardGateway = CreateWardGateway();
            var auditRepo = new Mock<IAuditRepo>();

            CreateOpnameHandler = new AdmCreateOpnameRequestHandler(
                opnameRepo.Object, patientGateway.Object, doctorGateway.Object, auditRepo.Object, TestTglJamProvider.Instance);
            var orchestrator = CreateOrchestrator();
            ProcessOpnameHandler = new AdmProcessOpnameRequestHandler(orchestrator.Object);
            CreateReservationHandler = new AdmCreateReservationHandler(
                reservationRepo.Object, patientGateway.Object, wardGateway.Object, auditRepo.Object, TestTglJamProvider.Instance);
            MaintainReservationHandler = new AdmMaintainReservationHandler(
                reservationRepo.Object, wardGateway.Object, auditRepo.Object, TestTglJamProvider.Instance);
            ProcessReservationHandler = new AdmProcessReservationHandler(orchestrator.Object);
            CreateWaitingListHandler = new AdmCreateWaitingListHandler(
                waitingListRepo.Object, admissionRepo.Object, wardGateway.Object, auditRepo.Object, TestTglJamProvider.Instance);
        }

        private Mock<IAdmissionRegistrationOrchestrator> CreateOrchestrator()
        {
            var mock = new Mock<IAdmissionRegistrationOrchestrator>();
            mock.Setup(x => x.ProcessOpnameRequest(
                    It.IsAny<AdmProcessOpnameRequestCmd>(), It.IsAny<CancellationToken>()))
                .Returns((AdmProcessOpnameRequestCmd cmd, CancellationToken _) =>
                {
                    var opname = Opnames[cmd.OpnameRequestId];
                    var admission = AdmissionModel.Admit(opname.Pasien,
                        new KelasDkType(cmd.KelasDkId, $"Kelas DK {cmd.KelasDkId}"),
                        new BangsalReff(cmd.BangsalId, $"Bangsal {cmd.BangsalId}"),
                        opname.OpnameRequestId, null, cmd.UserId);
                    Admissions[admission.RegId] = admission;
                    Opnames[opname.OpnameRequestId] = opname.Fulfill(admission.RegId, cmd.UserId);
                    return Task.FromResult(new AdmProcessAdmissionResponse(
                        admission.RegId, admission.AdmissionStatus));
                });
            mock.Setup(x => x.ProcessReservation(
                    It.IsAny<AdmProcessReservationCmd>(), It.IsAny<CancellationToken>()))
                .Returns((AdmProcessReservationCmd cmd, CancellationToken _) =>
                {
                    var reservation = Reservations[cmd.ReservationId];
                    var admission = AdmissionModel.Admit(reservation.Pasien,
                        new KelasDkType(cmd.KelasDkId, $"Kelas DK {cmd.KelasDkId}"),
                        new BangsalReff(cmd.BangsalId, $"Bangsal {cmd.BangsalId}"),
                        null, reservation.ReservationId, cmd.UserId);
                    Admissions[admission.RegId] = admission;
                    Reservations[reservation.ReservationId] = reservation.Realize(admission.RegId, cmd.UserId);
                    return Task.FromResult(new AdmProcessAdmissionResponse(
                        admission.RegId, admission.AdmissionStatus));
                });
            return mock;
        }

        private Mock<IOpnameRequestRepo> CreateOpnameRepo()
        {
            var mock = new Mock<IOpnameRequestRepo>();
            mock.Setup(x => x.SaveChanges(It.IsAny<OpnameRequestModel>()))
                .Callback<OpnameRequestModel>(m => Opnames[m.OpnameRequestId] = m);
            mock.Setup(x => x.LoadEntity(It.IsAny<IOpnameRequestKey>()))
                .Returns((IOpnameRequestKey k) =>
                    Opnames.TryGetValue(k.OpnameRequestId, out var m)
                        ? MayBe.From(m)
                        : MayBe<OpnameRequestModel>.None);
            return mock;
        }

        private Mock<IReservationRepo> CreateReservationRepo()
        {
            var mock = new Mock<IReservationRepo>();
            mock.Setup(x => x.SaveChanges(It.IsAny<ReservationModel>()))
                .Callback<ReservationModel>(m => Reservations[m.ReservationId] = m);
            mock.Setup(x => x.LoadEntity(It.IsAny<IReservationKey>()))
                .Returns((IReservationKey k) =>
                    Reservations.TryGetValue(k.ReservationId, out var m)
                        ? MayBe.From(m)
                        : MayBe<ReservationModel>.None);
            return mock;
        }

        private Mock<IAdmissionRepo> CreateAdmissionRepo()
        {
            var mock = new Mock<IAdmissionRepo>();
            mock.Setup(x => x.SaveChanges(It.IsAny<AdmissionModel>()))
                .Callback<AdmissionModel>(m => Admissions[m.RegId] = m);
            mock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
                .Returns((IRegKey k) =>
                    Admissions.TryGetValue(k.RegId, out var m)
                        ? MayBe.From(m)
                        : MayBe<AdmissionModel>.None);
            mock.Setup(x => x.ListData(It.IsAny<AdmissionListFilter>()))
                .Returns((AdmissionListFilter f) =>
                    Admissions.Values.Where(a =>
                        (f.PasienId == null || a.Pasien.PasienId == f.PasienId) &&
                        (f.Status == null || a.AdmissionStatus == f.Status)));
            return mock;
        }

        private Mock<IWaitingListRepo> CreateWaitingListRepo()
        {
            var mock = new Mock<IWaitingListRepo>();
            mock.Setup(x => x.SaveChanges(It.IsAny<WaitingListModel>()))
                .Callback<WaitingListModel>(m => WaitingLists[m.WaitingListId] = m);
            mock.Setup(x => x.HasActiveByRegId(It.IsAny<string>()))
                .Returns((string regId) =>
                    WaitingLists.Values.Any(w =>
                        w.RegId == regId &&
                        w.WaitingListStatus is WaitingListStatusEnum.Waiting
                            or WaitingListStatusEnum.Accepted));
            return mock;
        }

        private static Mock<IPatientAdministrationGateway> CreatePatientGateway()
        {
            var mock = new Mock<IPatientAdministrationGateway>();
            mock.Setup(x => x.ResolvePatient("P001"))
                .Returns(new PasienReff("P001", "Pasien WF", new DateOnly(1990, 1, 1), "L"));
            return mock;
        }

        private static Mock<IDoctorServiceGateway> CreateDoctorGateway()
        {
            var mock = new Mock<IDoctorServiceGateway>();
            mock.Setup(x => x.ResolveDoctor("D001"))
                .Returns(new PpaReff("D001", "Dr. WF"));
            return mock;
        }

        private static Mock<IWardAccommodationGateway> CreateWardGateway()
        {
            var mock = new Mock<IWardAccommodationGateway>();
            mock.Setup(x => x.ResolveKelas("K1")).Returns(new KelasReff("K1", "Kelas 1"));
            mock.Setup(x => x.ResolveKelas("K2")).Returns(new KelasReff("K2", "Kelas 2"));
            mock.Setup(x => x.ResolveKelasDk("1")).Returns(new KelasDkType("1", "Kelas DK 1"));
            mock.Setup(x => x.ResolveKelasDk("2")).Returns(new KelasDkType("2", "Kelas DK 2"));
            mock.Setup(x => x.ResolveBangsal("B1")).Returns(new BangsalReff("B1", "Bangsal A"));
            mock.Setup(x => x.ResolveBangsal("B2")).Returns(new BangsalReff("B2", "Bangsal B"));
            mock.Setup(x => x.ResolveBangsalForCareClass("B1", "1")).Returns(new BangsalReff("B1", "Bangsal A"));
            mock.Setup(x => x.ResolveBangsalForCareClass("B2", "2")).Returns(new BangsalReff("B2", "Bangsal B"));
            mock.Setup(x => x.ListEligibleBangsal("1")).Returns([new BangsalReff("B1", "Bangsal A")]);
            mock.Setup(x => x.ListEligibleBangsal("2")).Returns([new BangsalReff("B2", "Bangsal B")]);
            return mock;
        }
    }

    private static AdmissionRegistrationData RegistrationData() =>
        new("J1", "CM1", "IGD", "R1", "D1", "PESERTA1");
}
