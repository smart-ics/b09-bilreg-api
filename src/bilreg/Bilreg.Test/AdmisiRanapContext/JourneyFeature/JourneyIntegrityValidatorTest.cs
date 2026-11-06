using Bilreg.Application.AdmisiRanapContext.JourneyFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.JourneyFeature;

public class JourneyIntegrityValidatorTest
{
    private static readonly DateTime T0 = new(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T1 = new(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc);

    private static JourneyPatientSummary Patient(string id = "P001") =>
        new(id, "RM001", "Pasien Uji", "L", new DateOnly(1990, 1, 1));

    private static AdmissionJourneyFact CreateAdmission(
        string regId,
        AdmissionStatusEnum status = AdmissionStatusEnum.Admitted,
        string opnameRequestId = "-",
        string reservationId = "-") =>
        new(regId, status, opnameRequestId, reservationId, T1, "B01", "Bangsal Melati", T0, null);

    private static JourneyNormalizedFacts CreateFacts(
        JourneyOriginKind origin,
        AdmissionJourneyFact[]? admissions = null,
        OpnameRequestJourneyFact? opn = null,
        ReservationJourneyFact? rsv = null,
        WaitingListJourneyFact[]? waitingLists = null,
        bool orphanRegistration = false) =>
        new(
            origin,
            Patient(),
            opn,
            rsv,
            admissions ?? [],
            waitingLists ?? [],
            orphanRegistration);

    [Fact]
    public void ValidateAdmissionSources_WhenOpnameAndReservationIdsAreIdentical_DoesNotTriggerAdmissionHasBothSources()
    {
        // Arrange - Admisi berasal dari satu pesanan rawat inap terpadu dengan ID sama (e.g. OPN0A31CDRTQ)
        const string unifiedOrderId = "OPN0A31CDRTQ";
        var admission = CreateAdmission("RG0A31CDRTQ", opnameRequestId: unifiedOrderId, reservationId: unifiedOrderId);
        var facts = CreateFacts(JourneyOriginKind.Reservation, admissions: [admission]);

        // Act
        var result = JourneyIntegrityValidator.Validate(facts);

        // Assert
        result.Issues.Should().NotContain(i => i.Code == JourneyReconciliationCodes.AdmissionHasBothSources);
        result.HasBlockingIssues.Should().BeFalse();
    }

    [Fact]
    public void ValidateAdmissionSources_WhenOpnameAndReservationIdsAreDistinct_TriggersAdmissionHasBothSources()
    {
        // Arrange - Admisi mereferensikan dua sumber berbeda yang bertentangan
        var admission = CreateAdmission("RG001", opnameRequestId: "OPN123", reservationId: "RSV456");
        var facts = CreateFacts(JourneyOriginKind.DirectOrLegacyAdmission, admissions: [admission]);

        // Act
        var result = JourneyIntegrityValidator.Validate(facts);

        // Assert
        result.Issues.Should().ContainSingle(i => i.Code == JourneyReconciliationCodes.AdmissionHasBothSources);
        result.HasBlockingIssues.Should().BeTrue();
        var issue = result.Issues.Single(i => i.Code == JourneyReconciliationCodes.AdmissionHasBothSources);
        issue.RelatedRecordIds.Should().Contain(["RG001", "OPN123", "RSV456"]);
    }

    [Fact]
    public void ValidateAdmissionSources_WhenOnlyOpnameRequestIdPresent_DoesNotTriggerAdmissionHasBothSources()
    {
        // Arrange
        var admission = CreateAdmission("RG001", opnameRequestId: "OPN123", reservationId: "-");
        var facts = CreateFacts(JourneyOriginKind.OpnameRequest, admissions: [admission]);

        // Act
        var result = JourneyIntegrityValidator.Validate(facts);

        // Assert
        result.Issues.Should().NotContain(i => i.Code == JourneyReconciliationCodes.AdmissionHasBothSources);
    }

    [Fact]
    public void ValidateAdmissionSources_WhenOnlyReservationIdPresent_DoesNotTriggerAdmissionHasBothSources()
    {
        // Arrange
        var admission = CreateAdmission("RG001", opnameRequestId: "-", reservationId: "RSV456");
        var facts = CreateFacts(JourneyOriginKind.Reservation, admissions: [admission]);

        // Act
        var result = JourneyIntegrityValidator.Validate(facts);

        // Assert
        result.Issues.Should().NotContain(i => i.Code == JourneyReconciliationCodes.AdmissionHasBothSources);
    }

    [Fact]
    public void ValidateAdmissionSources_WhenMultipleAdmissionsPresent_TriggersMultipleAdmissionsForSource()
    {
        // Arrange
        var adm1 = CreateAdmission("RG001", opnameRequestId: "OPN100");
        var adm2 = CreateAdmission("RG002", opnameRequestId: "OPN100");
        var facts = CreateFacts(JourneyOriginKind.OpnameRequest, admissions: [adm1, adm2]);

        // Act
        var result = JourneyIntegrityValidator.Validate(facts);

        // Assert
        result.Issues.Should().Contain(i => i.Code == JourneyReconciliationCodes.MultipleAdmissionsForSource);
    }

    [Fact]
    public void JourneyIdDerivation_WithUnifiedInpatientOrderId_DerivesConsistentReservationPrefix()
    {
        // Skenario pasien reservasi terpadu (seperti Sri Ajeng Retno)
        const string unifiedOrderId = "OPN0A31CDRTQ";
        var admission = CreateAdmission("RG0A31CDRTQ", opnameRequestId: unifiedOrderId, reservationId: unifiedOrderId);
        var rsvFact = new ReservationJourneyFact(
            unifiedOrderId,
            ReservationStatusEnum.Realized,
            "RG0A31CDRTQ",
            T1,
            T0);

        var facts = CreateFacts(
            JourneyOriginKind.Reservation,
            admissions: [admission],
            rsv: rsvFact);

        // Act
        var integrity = JourneyIntegrityValidator.Validate(facts);
        var journeyId = JourneyIdFactory.Derive(facts, integrity);

        // Assert
        integrity.HasBlockingIssues.Should().BeFalse();
        journeyId.Should().Be("rsv:OPN0A31CDRTQ");
    }

    [Fact]
    public void JourneyIdDerivation_WithUnifiedInpatientOrderId_DerivesConsistentOpnamePrefix()
    {
        // Skenario pasien opname terpadu
        const string unifiedOrderId = "OPN0A31CDRTQ";
        var admission = CreateAdmission("RG0A31CDRTQ", opnameRequestId: unifiedOrderId, reservationId: unifiedOrderId);
        var opnFact = new OpnameRequestJourneyFact(
            unifiedOrderId,
            OpnameRequestStatusEnum.Fulfilled,
            "RG0A31CDRTQ",
            T1,
            T0);

        var facts = CreateFacts(
            JourneyOriginKind.OpnameRequest,
            admissions: [admission],
            opn: opnFact);

        // Act
        var integrity = JourneyIntegrityValidator.Validate(facts);
        var journeyId = JourneyIdFactory.Derive(facts, integrity);

        // Assert
        integrity.HasBlockingIssues.Should().BeFalse();
        journeyId.Should().Be("opn:OPN0A31CDRTQ");
    }

    [Fact]
    public void JourneyIdDerivation_WhenIntegrityFailsDueToDistinctSources_FallsBackToRegPrefix()
    {
        // Arrange
        var admission = CreateAdmission("RG001", opnameRequestId: "OPN100", reservationId: "RSV200");
        var facts = CreateFacts(JourneyOriginKind.Reservation, admissions: [admission]);

        // Act
        var integrity = JourneyIntegrityValidator.Validate(facts);
        var journeyId = JourneyIdFactory.Derive(facts, integrity);

        // Assert
        integrity.HasBlockingIssues.Should().BeTrue();
        journeyId.Should().Be("reg:RG001");
    }
}
