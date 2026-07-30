using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class AdmisiRajalOfficerWorklistReferenceReaderTest
{
    [Fact]
    public void Resolve_PrefersAssistanceBookingAndEstablishedOutcome()
    {
        var result = Resolve(new(
            "AQ-1",
            1,
            "B-ASSIST",
            "B-TRACKER",
            "B-QUEUE",
            "R-OUTCOME",
            "R-QUEUE",
            "R-TRACKER",
            "R-BOOKING"));

        result.References.BookingId.Should().Be("B-ASSIST");
        result.References.RegistrationId.Should().Be("R-OUTCOME");
    }

    [Fact]
    public void Resolve_FallsBackToTrackerBookingAndQueueRegistration()
    {
        var result = Resolve(new(
            "AQ-1",
            1,
            null,
            "B-TRACKER",
            "B-QUEUE",
            null,
            "R-QUEUE",
            "R-TRACKER",
            "R-BOOKING"));

        result.References.BookingId.Should().Be("B-TRACKER");
        result.References.RegistrationId.Should().Be("R-QUEUE");
    }

    [Fact]
    public void Resolve_FallsBackToQueueBookingAndTrackerRegistration()
    {
        var result = Resolve(new(
            "AQ-1",
            1,
            null,
            null,
            "B-QUEUE",
            null,
            null,
            "R-TRACKER",
            "R-BOOKING"));

        result.References.BookingId.Should().Be("B-QUEUE");
        result.References.RegistrationId.Should().Be("R-TRACKER");
    }

    [Fact]
    public void Resolve_FallsBackToBookingRegistrationAndNormalizesSentinels()
    {
        var result = Resolve(new(
            "AQ-1",
            1,
            " - ",
            "",
            "  ",
            null,
            "-",
            " ",
            " R-BOOKING "));

        result.References.BookingId.Should().BeNull();
        result.References.RegistrationId.Should().Be("R-BOOKING");
    }

    private static AdmisiRajalOfficerWorklistReferenceView Resolve(
        AdmisiRajalOfficerWorklistReferenceSource source) =>
        AdmisiRajalOfficerWorklistReferenceReader.Resolve(source);
}
