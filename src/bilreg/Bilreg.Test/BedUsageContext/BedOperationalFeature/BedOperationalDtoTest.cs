using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Infrastructure.BedUsageContext.BedOperationalFeature;
using FluentAssertions;

namespace Bilreg.Test.BedUsageContext.BedOperationalFeature;

public class BedOperationalDtoTest
{
    private static readonly DateTime At =
        new(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RoundTrip_UnverifiedBed_PreservesNullableProjection()
    {
        var source = Create();

        var result = BedOperationalDto.FromModel(source).ToModel([], []);

        result.Should().BeEquivalentTo(source);
        result.CurrentReadiness.Should().BeNull();
        result.LatestReadinessTransactionId.Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_CorrectedReadiness_PreservesAggregateAndUtcHistory()
    {
        var ready = Create().VerifyReady(
            "Ready verified",
            "EVIDENCE-1",
            "PEG-1",
            "PEG-2",
            At,
            At.AddMinutes(1),
            "SOURCE-1",
            "REQ-1");
        var originalId = ready.ListReadinessTransaction.Single().TransactionId;
        var source = ready.CorrectReadiness(
            originalId,
            BedReadinessStatusEnum.Blocked,
            BedRestrictionTypeEnum.Safety,
            "Safety restriction remains",
            "Ready fact entered incorrectly",
            "EVIDENCE-2",
            "PEG-HEAD",
            string.Empty,
            At.AddHours(1),
            At.AddHours(1).AddMinutes(1),
            "REQ-2")
            .AdvanceOccupancyEpoch();
        var dto = BedOperationalDto.FromModel(source);
        var transactions = source.ListReadinessTransaction
            .Select(BedReadinessTransactionDto.FromModel)
            .Select(x => x.ToModel());
        var corrections = source.ListReadinessCorrection
            .Select(BedReadinessCorrectionDto.FromModel)
            .Select(x => x.ToModel());

        var result = dto.ToModel(transactions, corrections);

        result.Should().BeEquivalentTo(source);
        result.CurrentReadiness.Should().Be(BedReadinessStatusEnum.Blocked);
        result.CurrentBlockerState.RestrictionType
            .Should().Be(BedRestrictionTypeEnum.Safety);
        result.OccupancyEpoch.Should().Be(1);
        result.ListReadinessTransaction.Should()
            .OnlyContain(x =>
                x.OccurredAt.Kind == DateTimeKind.Utc &&
                x.RecordedAt.Kind == DateTimeKind.Utc);
        result.ListReadinessCorrection.Should()
            .OnlyContain(x =>
                x.OccurredAt.Kind == DateTimeKind.Utc &&
                x.RecordedAt.Kind == DateTimeKind.Utc);
    }

    private static BedOperationalModel Create() =>
        BedOperationalModel.Create(
            "BED00001",
            "B1",
            "K001",
            new OccupancyPolicyReff("POL-1", "Standard"));
}
