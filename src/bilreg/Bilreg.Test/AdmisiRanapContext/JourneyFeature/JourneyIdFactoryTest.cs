using Bilreg.Application.AdmisiRanapContext.JourneyFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.JourneyFeature;

public class JourneyIdFactoryTest
{
    [Theory]
    [InlineData(OrderSourceEnum.EmrRawatJalan, "opn:OPN001")]
    [InlineData(OrderSourceEnum.EmrIgd, "opn:OPN001")]
    [InlineData(OrderSourceEnum.AdmisiBooking, "rsv:OPN001")]
    [InlineData(OrderSourceEnum.ExternalChannel, "rsv:OPN001")]
    public void UT01_FromInpatientOrder_ReturnsDeterministicPrefixBasedOnSource(
        OrderSourceEnum source,
        string expected)
    {
        var actual = JourneyIdFactory.FromInpatientOrder("OPN001", source);
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "opn:OPN002")]
    [InlineData(2, "opn:OPN002")]
    [InlineData(3, "rsv:OPN002")]
    [InlineData(4, "rsv:OPN002")]
    public void UT02_FromOrderSource_WithInt_ReturnsDeterministicPrefix(
        int source,
        string expected)
    {
        var actual = JourneyIdFactory.FromOrderSource("OPN002", source);
        actual.Should().Be(expected);
    }

    [Fact]
    public void UT03_FromOpnameRequest_ReturnsOpnPrefix()
    {
        var actual = JourneyIdFactory.FromOpnameRequest("OPN123");
        actual.Should().Be("opn:OPN123");
    }

    [Fact]
    public void UT04_FromReservation_ReturnsRsvPrefix()
    {
        var actual = JourneyIdFactory.FromReservation("RSV123");
        actual.Should().Be("rsv:RSV123");
    }

    [Fact]
    public void UT05_FromRegId_ReturnsRegPrefix()
    {
        var actual = JourneyIdFactory.FromRegId("REG123");
        actual.Should().Be("reg:REG123");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-")]
    public void UT06_FromInpatientOrder_GivenInvalidId_ThrowsArgumentException(string invalidId)
    {
        var act = () => JourneyIdFactory.FromInpatientOrder(invalidId, OrderSourceEnum.EmrRawatJalan);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UT07_Derive_GivenInpatientOrderClinical_DerivesOpnPrefix()
    {
        var facts = JourneyNormalizedFacts.Empty(JourneyOriginKind.OpnameRequest) with
        {
            InpatientOrder = new InpatientOrderJourneyFact(
                "OPN-CLINIC-01",
                OrderSourceEnum.EmrRawatJalan,
                InpatientOrderStatusEnum.Requested,
                "-")
        };

        var actual = JourneyIdFactory.Derive(facts, JourneyIntegrityResult.Empty);
        actual.Should().Be("opn:OPN-CLINIC-01");
    }

    [Fact]
    public void UT08_Derive_GivenInpatientOrderBooking_DerivesRsvPrefix()
    {
        var facts = JourneyNormalizedFacts.Empty(JourneyOriginKind.Reservation) with
        {
            InpatientOrder = new InpatientOrderJourneyFact(
                "OPN-BOOK-01",
                OrderSourceEnum.AdmisiBooking,
                InpatientOrderStatusEnum.Requested,
                "-")
        };

        var actual = JourneyIdFactory.Derive(facts, JourneyIntegrityResult.Empty);
        actual.Should().Be("rsv:OPN-BOOK-01");
    }

    [Fact]
    public void UT09_Derive_GivenAdmissionWithInpatientOrder_DerivesPrefixFromSource()
    {
        var admission = new AdmissionJourneyFact(
            "REG001",
            AdmissionStatusEnum.Admitted,
            "-",
            "-",
            InpatientOrderId: "OPN999",
            OrderSource: OrderSourceEnum.EmrIgd);

        var facts = JourneyNormalizedFacts.Empty(JourneyOriginKind.DirectOrLegacyAdmission) with
        {
            Admissions = new[] { admission }
        };

        var actual = JourneyIdFactory.Derive(facts, JourneyIntegrityResult.Empty);
        actual.Should().Be("opn:OPN999");
    }

    [Fact]
    public void UT10_Derive_GivenIntegrityConflict_FallsBackToRegPrefix()
    {
        var admission = new AdmissionJourneyFact("REG001", AdmissionStatusEnum.Admitted, "-", "-");
        var facts = JourneyNormalizedFacts.Empty(JourneyOriginKind.OpnameRequest) with
        {
            Admissions = new[] { admission }
        };
        var conflictIntegrity = new JourneyIntegrityResult(new[]
        {
            new JourneyReconciliationIssue("CONFLICT", "Conflict detected", Array.Empty<string>())
        });

        var actual = JourneyIdFactory.Derive(facts, conflictIntegrity);
        actual.Should().Be("reg:REG001");
    }

    [Theory]
    [InlineData("opn:OPN100", true, "opn:", "OPN100")]
    [InlineData("rsv:RSV200", true, "rsv:", "RSV200")]
    [InlineData("reg:REG300", true, "reg:", "REG300")]
    [InlineData("unknown:123", false, "", "")]
    [InlineData("", false, "", "")]
    public void UT11_TryParse_ParsesPrefixAndDomainIdCorrectly(
        string journeyId,
        bool expectedSuccess,
        string expectedPrefix,
        string expectedDomainId)
    {
        var success = JourneyIdFactory.TryParse(journeyId, out var prefix, out var domainId);
        success.Should().Be(expectedSuccess);
        prefix.Should().Be(expectedPrefix);
        domainId.Should().Be(expectedDomainId);
    }

    [Fact]
    public void UT12_ResolveLegacyRecord_ResolvesInpatientOrderWithKnownFacts()
    {
        var facts = JourneyNormalizedFacts.Empty(JourneyOriginKind.OpnameRequest) with
        {
            InpatientOrder = new InpatientOrderJourneyFact(
                "OPN005",
                OrderSourceEnum.EmrRawatJalan,
                InpatientOrderStatusEnum.Requested,
                "-")
        };

        var actual = JourneyIdFactory.ResolveLegacyRecord("inpatientorder", "OPN005", facts);
        actual.Should().Be("opn:OPN005");
    }
}
