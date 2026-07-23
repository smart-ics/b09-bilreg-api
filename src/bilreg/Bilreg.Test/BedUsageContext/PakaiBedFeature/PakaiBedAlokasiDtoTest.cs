using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;
using FluentAssertions;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedAlokasiDtoTest
{
    private static readonly DateTime ProposedAt = new(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FromModelToModel_ActiveAllocation_PreservesAggregateAndFacts()
    {
        var source = CreateActive();

        var dto = PakaiBedAlokasiDto.FromModel(source);
        var result = dto.ToModel(
            source.ListTransition.Select(PakaiBedTransisiDto.FromModel).Select(x => x.ToModel()),
            source.ListCorrection.Select(PakaiBedKoreksiDto.FromModel).Select(x => x.ToModel()));

        result.Should().BeEquivalentTo(source);
        result.StartedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        result.ListTransition.Should().OnlyContain(x => x.OccurredAt.Kind == DateTimeKind.Utc);
    }

    [Fact]
    public void ToModel_EmptyStartedAtSentinel_ReturnsNull()
    {
        var source = PakaiBedAlokasiModel.Propose(
            "REG-1", "P-1", "BG", "KMR-1", "BED-1", "WTL-1", "REQ-1",
            PakaiBedPurposeEnum.Clinical, OccupantRoleEnum.Primary,
            "USR-1", ProposedAt, ProposedAt.AddMinutes(1), "Proposal");

        var result = PakaiBedAlokasiDto.FromModel(source).ToModel([], []);

        result.StartedAt.Should().BeNull();
    }

    private static PakaiBedAlokasiModel CreateActive() =>
        PakaiBedAlokasiModel.Propose(
                "REG-1", "P-1", "BG", "KMR-1", "BED-1", "WTL-1", "REQ-1",
                PakaiBedPurposeEnum.Clinical, OccupantRoleEnum.Primary,
                "USR-1", ProposedAt, ProposedAt.AddMinutes(1), "Proposal")
            .Assign("USR-2", ProposedAt.AddMinutes(5), ProposedAt.AddMinutes(6),
                "Assignment", "EVIDENCE-1");
}
