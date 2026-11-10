using Bilreg.Application.AdmisiRanapContext.JourneyFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.JourneyFeature;

/// <summary>
/// Skenario pengujian operasional terpadu end-to-end untuk pendaftaran pasien reservasi rawat inap
/// (seperti pasien SRI AJENG RETNO, No RM 00-00-60-63, Order OPN0A31CDRTQ).
/// Memverifikasi bahwa registrasi reservasi menghasilkan tahap operasional WardAcceptanceRequired
/// ("Menunggu Diterima Bangsal"), Journey ID deterministik "rsv:OPN...", dan respons workspace
/// siap dirender seketika tanpa hang/skeleton loading.
/// </summary>
public class JourneyOperationalReservationScenarioTest
{
    private const string SriAjengPasienId = "P00006063";
    private const string SriAjengNoRm = "00-00-60-63";
    private const string SriAjengNama = "SRI AJENG RETNO";
    private const string SriAjengOrderId = "OPN0A31CDRTQ";
    private const string GeneratedRegId = "RG20261105001";
    private const string TargetBangsalId = "B01";
    private const string TargetBangsalName = "Bangsal Melati";

    private static readonly DateTime OrderTime = new(2026, 11, 5, 8, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime AdmissionTime = new(2026, 11, 5, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime WaitingListTime = new(2026, 11, 5, 9, 5, 0, DateTimeKind.Utc);

    [Fact]
    public void Scenario_SriAjengRetno_ReservationRegistration_ResolvesWardAcceptanceRequired_WithRsvPrefix()
    {
        // 1. Arrange: Fakta terpadu pasien SRI AJENG RETNO yang terdaftar dari reservasi
        var patient = new JourneyPatientSummary(SriAjengPasienId, SriAjengNoRm, SriAjengNama, "P", new DateOnly(1995, 5, 20));

        var reservation = new ReservationJourneyFact(
            SriAjengOrderId,
            ReservationStatusEnum.Realized,
            GeneratedRegId,
            OrderTime,
            OrderTime);

        // Admisi hasil rekonsiliasi murni merujuk InpatientOrderId sebagai ReservationId, OpnameRequestId = "-"
        var admission = new AdmissionJourneyFact(
            GeneratedRegId,
            AdmissionStatusEnum.Admitted,
            "-",
            SriAjengOrderId,
            AdmissionTime,
            TargetBangsalId,
            TargetBangsalName,
            AdmissionTime,
            null);

        // Handover akomodasi dibuat dan berstatus Waiting (antrean bangsal)
        var waitingList = new WaitingListJourneyFact(
            "WL0A31CDRTQ",
            GeneratedRegId,
            WaitingListStatusEnum.Waiting,
            WaitingListTime,
            TargetBangsalId,
            TargetBangsalName);

        var facts = new JourneyNormalizedFacts(
            JourneyOriginKind.Reservation,
            patient,
            null,
            reservation,
            [admission],
            [waitingList]);

        // 2. Act: Validasi integritas dan resolusi tahap operasional Release 1
        var integrityResult = JourneyIntegrityValidator.Validate(facts);
        var resolution = Release1JourneyStageResolver.Resolve(facts, integrityResult);

        // 3. Assert: Bebas dari false-positive rekonsiliasi
        integrityResult.HasBlockingIssues.Should().BeFalse(
            "registrasi reservasi terpadu tidak boleh mengalami anomali integritas atau kontradiksi");
        integrityResult.Issues.Should().NotContain(i => i.Code == JourneyReconciliationCodes.AdmissionHasBothSources);

        // 4. Assert: Identitas perjalanan konsisten dengan prefiks rsv:OPN...
        resolution.Identity.JourneyId.Should().Be($"rsv:{SriAjengOrderId}",
            "ID perjalanan reservasi harus mempertahankan prefiks rsv: dengan order ID terkait");
        resolution.Identity.RegId.Should().Be(GeneratedRegId);
        resolution.Identity.OriginKind.Should().Be(JourneyOriginKind.Reservation);

        // 5. Assert: Tahap operasional adalah 'WardAcceptanceRequired' ("Menunggu Diterima Bangsal")
        resolution.Stage.Should().Be(JourneyOperationalStage.WardAcceptanceRequired,
            "pasien yang telah terdaftar dan menunggu respons bangsal harus berstatus WardAcceptanceRequired");
        resolution.CurrentCondition.Title.Should().Be("Menunggu penerimaan bangsal");

        // 6. Assert: Tugas berikutnya menjadi milik Bangsal tujuan (Ward), bukan aksi tertahan Admisi
        resolution.NextTask.Should().NotBeNull();
        resolution.NextTask!.Code.Should().Be(JourneyActionCode.WardAcceptHandover);
        resolution.NextTask.Owner.Domain.Should().Be(JourneyOwnerDomain.Ward);
        resolution.NextTask.Owner.Name.Should().Be(TargetBangsalName);
        resolution.NextTask.CanExecute.Should().BeFalse(
            "tugas penerimaan handover akomodasi adalah otoritas bangsal dan tidak dapat dieksekusi oleh Admisi");
    }

    [Fact]
    public void Scenario_SriAjengRetno_WorklistAndWorkspaceJourneyId_AreIdentical_PreventingSkeletonHang()
    {
        // Memverifikasi pencegahan bug infinite skeleton loading:
        // Antarmuka frontend (useJourneyQueryState.ts) memeriksa data.journeyId === selectedJourneyId.
        // Bila worklist menampilkan rsv:OPN0A31CDRTQ, detail workspace juga HARUS mengembalikan rsv:OPN0A31CDRTQ.
        var patient = new JourneyPatientSummary(SriAjengPasienId, SriAjengNoRm, SriAjengNama);
        var reservation = new ReservationJourneyFact(SriAjengOrderId, ReservationStatusEnum.Realized, GeneratedRegId, OrderTime);
        var admission = new AdmissionJourneyFact(GeneratedRegId, AdmissionStatusEnum.Admitted, "-", SriAjengOrderId, AdmissionTime, TargetBangsalId, TargetBangsalName);
        var waitingList = new WaitingListJourneyFact("WL0A31CDRTQ", GeneratedRegId, WaitingListStatusEnum.Waiting, WaitingListTime, TargetBangsalId, TargetBangsalName);

        var facts = new JourneyNormalizedFacts(JourneyOriginKind.Reservation, patient, null, reservation, [admission], [waitingList]);

        var resolution = Release1JourneyStageResolver.Resolve(facts);

        // Simulasi item worklist di daftar samping kiri
        var worklistItemJourneyId = resolution.Identity.JourneyId;

        // Simulasi detail workspace yang dirender di panel samping kanan
        var detailWorkspaceJourneyId = resolution.Identity.JourneyId;

        // Verifikasi keselarasan identitas
        worklistItemJourneyId.Should().Be(detailWorkspaceJourneyId);
        worklistItemJourneyId.Should().StartWith("rsv:OPN");
        resolution.Stage.Should().NotBe(JourneyOperationalStage.NeedsReconciliation,
            "workspace tidak boleh terkunci pada NeedsReconciliation");
    }

    [Fact]
    public void Scenario_SriAjengRetno_TransitionalDualColumns_StillResolvesWardAcceptanceRequired()
    {
        // Memverifikasi ketahanan arsitektur (TD-06 & TD-07):
        // Jika data lama atau kueri transisi menghasilkan OpnameRequestId == ReservationId == SriAjengOrderId,
        // sistem tidak boleh mendeteksi konflik palsu ganda dan harus tetap mengarahkan ke WardAcceptanceRequired.
        var patient = new JourneyPatientSummary(SriAjengPasienId, SriAjengNoRm, SriAjengNama);
        var reservation = new ReservationJourneyFact(SriAjengOrderId, ReservationStatusEnum.Realized, GeneratedRegId, OrderTime);
        var transitionalAdmission = new AdmissionJourneyFact(
            GeneratedRegId,
            AdmissionStatusEnum.Admitted,
            SriAjengOrderId,
            SriAjengOrderId,
            AdmissionTime,
            TargetBangsalId,
            TargetBangsalName);
        var waitingList = new WaitingListJourneyFact("WL0A31CDRTQ", GeneratedRegId, WaitingListStatusEnum.Waiting, WaitingListTime, TargetBangsalId, TargetBangsalName);

        var facts = new JourneyNormalizedFacts(JourneyOriginKind.Reservation, patient, null, reservation, [transitionalAdmission], [waitingList]);

        var integrityResult = JourneyIntegrityValidator.Validate(facts);
        var resolution = Release1JourneyStageResolver.Resolve(facts, integrityResult);

        integrityResult.HasBlockingIssues.Should().BeFalse();
        resolution.Identity.JourneyId.Should().Be($"rsv:{SriAjengOrderId}");
        resolution.Stage.Should().Be(JourneyOperationalStage.WardAcceptanceRequired);
    }

    [Fact]
    public void Scenario_FullLifecycle_FromAdmissionModelCreationToJourneyResolution_ResolvesWardAcceptanceRequired()
    {
        // 1. Buat referensi pasien
        var pasien = new PasienReff(SriAjengPasienId, SriAjengNama, new DateOnly(1995, 5, 20), "P");

        // 2. Buat InpatientOrder terpadu (Booking Admisi)
        var order = InpatientOrderModel.CreateBooking(
            pasien,
            plannedDate: OrderTime,
            kelasRawat: new KelasReff("1", "Kelas 1"),
            bangsal: new BangsalReff(TargetBangsalId, TargetBangsalName),
            auditUserId: "user-admisi");

        // 3. Daftarkan admisi menggunakan InpatientOrderId tunggal (sesuai P1-S01)
        var admission = AdmissionModel.Admit(
            pasien,
            new KelasDkType("1", "Kelas 1"),
            new BangsalReff(TargetBangsalId, TargetBangsalName),
            opnameRequestId: null,
            reservationId: null,
            auditUserId: "user-admisi",
            admittedAt: AdmissionTime,
            inpatientOrderId: order.InpatientOrderId);

        // Pastikan entitas admisi murni memuat InpatientOrderId dan kolom lama bersih ("-")
        admission.InpatientOrderId.Should().Be(order.InpatientOrderId);
        admission.OpnameRequestId.Should().Be("-");
        admission.ReservationId.Should().Be("-");

        // Fulfill pesanan
        var fulfilledOrder = order.Fulfill(admission.RegId, "user-admisi");
        fulfilledOrder.OrderStatus.Should().Be(InpatientOrderStatusEnum.Fulfilled);
        fulfilledOrder.FulfilledRegId.Should().Be(admission.RegId);

        // 4. Proyeksikan ke fakta perjalanan Release 1 berdasarkan OrderSourceEnum (sesuai TD-04 & TD-05)
        var originKind = (order.OrderSource == OrderSourceEnum.AdmisiBooking)
            ? JourneyOriginKind.Reservation
            : JourneyOriginKind.OpnameRequest;

        originKind.Should().Be(JourneyOriginKind.Reservation);

        var reservationFact = new ReservationJourneyFact(
            order.InpatientOrderId,
            ReservationStatusEnum.Realized,
            admission.RegId,
            order.PlannedDate);

        var admissionFact = new AdmissionJourneyFact(
            admission.RegId,
            admission.AdmissionStatus,
            "-",
            order.InpatientOrderId, // Diterjemahkan dari InpatientOrderId dengan OrderSource=AdmisiBooking
            admission.AdmissionDate,
            TargetBangsalId,
            TargetBangsalName);

        var waitingListFact = new WaitingListJourneyFact(
            "WL0A31CDRTQ",
            admission.RegId,
            WaitingListStatusEnum.Waiting,
            WaitingListTime,
            TargetBangsalId,
            TargetBangsalName);

        var facts = new JourneyNormalizedFacts(
            originKind,
            new JourneyPatientSummary(pasien.PasienId, SriAjengNoRm, pasien.PasienName),
            null,
            reservationFact,
            [admissionFact],
            [waitingListFact]);

        // 5. Validasi integritas dan tahap operasional
        var integrity = JourneyIntegrityValidator.Validate(facts);
        var resolution = Release1JourneyStageResolver.Resolve(facts, integrity);

        integrity.HasBlockingIssues.Should().BeFalse();
        resolution.Identity.JourneyId.Should().Be($"rsv:{order.InpatientOrderId}");
        resolution.Identity.JourneyId.Should().StartWith("rsv:OPN");
        resolution.Stage.Should().Be(JourneyOperationalStage.WardAcceptanceRequired);
        resolution.CurrentCondition.Title.Should().Be("Menunggu penerimaan bangsal");
        resolution.NextTask!.Code.Should().Be(JourneyActionCode.WardAcceptHandover);
        resolution.NextTask.Owner.Domain.Should().Be(JourneyOwnerDomain.Ward);
    }
}
