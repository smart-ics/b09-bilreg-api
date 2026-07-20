using Bilreg.Application.Shared;
using Bilreg.Application.Shared.BusinessDateFeature;
using FluentAssertions;

namespace Bilreg.Test.Shared.BusinessDateFeature;

public class GetBusinessDateStatusHandlerTest
{
    [Fact]
    public async Task FixedMode_ReturnsSimulationResponse()
    {
        var status = new StubStatus(
            "Fixed",
            new DateOnly(2025, 5, 3),
            new DateTime(2025, 5, 3, 10, 15, 30),
            new DateTime(2026, 7, 20, 10, 15, 30),
            true);

        var result = await new GetBusinessDateStatusHandler(status)
            .Handle(new GetBusinessDateStatusQry(), default);

        result.Should().Be(new BusinessDateStatusResponse(
            "Fixed", "2025-05-03", "2025-05-03T10:15:30",
            "2026-07-20", "2026-07-20T10:15:30", true));
    }

    [Fact]
    public async Task SystemMode_ReturnsSystemResponse()
    {
        var now = new DateTime(2026, 7, 20, 10, 15, 30);
        var status = new StubStatus("System", null, now, now, false);

        var result = await new GetBusinessDateStatusHandler(status)
            .Handle(new GetBusinessDateStatusQry(), default);

        result.Should().Be(new BusinessDateStatusResponse(
            "System", "2026-07-20", "2026-07-20T10:15:30",
            "2026-07-20", "2026-07-20T10:15:30", false));
    }

    private sealed record StubStatus(
        string Mode,
        DateOnly? FixedDate,
        DateTime BusinessNow,
        DateTime SystemNow,
        bool IsSimulation) : IBusinessDateStatus;
}
