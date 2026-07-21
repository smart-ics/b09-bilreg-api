using Bilreg.Domain.BedUsageContext.BedOperationalFeature;

namespace Bilreg.Infrastructure.BedUsageContext.BedOperationalFeature;

public record BedOperationalDto(
    string BedId,
    string BangsalId,
    string KamarId,
    string OccupancyPolicyId,
    string OccupancyPolicyName,
    int? CurrentReadiness,
    string LatestReadinessTransactionId,
    bool IsBlocked,
    int CurrentRestrictionType,
    string CurrentBlockerReason,
    string CurrentBlockerTransactionId,
    long OccupancyEpoch,
    int Version,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public static BedOperationalDto FromModel(BedOperationalModel model)
    {
        var facts = model.ListReadinessTransaction
            .Select(x => new AuditFact(x.ResponsibleActorId, x.RecordedAt))
            .Concat(model.ListReadinessCorrection.Select(
                x => new AuditFact(x.ActorId, x.RecordedAt)))
            .OrderBy(x => x.RecordedAt)
            .ToList();
        var first = facts.FirstOrDefault() ?? new AuditFact(string.Empty, EmptyDate);
        var last = facts.LastOrDefault() ?? first;

        return new BedOperationalDto(
            model.BedId,
            model.BangsalId,
            model.KamarId,
            model.OccupancyPolicy.OccupancyPolicyId,
            model.OccupancyPolicy.DisplayName,
            model.CurrentReadiness is null ? null : (int)model.CurrentReadiness.Value,
            model.LatestReadinessTransactionId,
            model.CurrentBlockerState.IsBlocked,
            (int)model.CurrentBlockerState.RestrictionType,
            model.CurrentBlockerState.Reason,
            model.CurrentBlockerState.OriginatingTransactionId,
            model.OccupancyEpoch,
            model.Version,
            first.UserId,
            first.RecordedAt,
            last.UserId,
            last.RecordedAt,
            string.Empty,
            EmptyDate);
    }

    public BedOperationalModel ToModel(
        IEnumerable<BedReadinessTransactionModel> transactions,
        IEnumerable<BedReadinessCorrectionModel> corrections) =>
        new(
            BedId,
            BangsalId,
            KamarId,
            new OccupancyPolicyReff(OccupancyPolicyId, OccupancyPolicyName),
            CurrentReadiness is null
                ? null
                : (BedReadinessStatusEnum)CurrentReadiness.Value,
            LatestReadinessTransactionId,
            new BedBlockerStateType(
                IsBlocked,
                (BedRestrictionTypeEnum)CurrentRestrictionType,
                CurrentBlockerReason,
                CurrentBlockerTransactionId),
            OccupancyEpoch,
            Version,
            transactions,
            corrections);

    private sealed record AuditFact(string UserId, DateTime RecordedAt);
}
