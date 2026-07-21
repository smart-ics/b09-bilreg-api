using Bilreg.Domain.BedUsageContext.PakaiBedFeature;

namespace Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;

public record PakaiBedAlokasiDto(
    string PakaiBedId,
    int Version,
    string RegId,
    string PasienId,
    string BangsalId,
    string KamarId,
    string BedId,
    string WaitingListId,
    string RequestId,
    int PakaiBedPurpose,
    int OccupantRole,
    int PakaiBedStatus,
    DateTime ProposedAt,
    DateTime StartedAt,
    string AssignedBy,
    string BedAssignabilityEvidenceId,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public static PakaiBedAlokasiDto FromModel(PakaiBedAlokasiModel model)
    {
        var facts = model.ListTransition
            .Select(x => new AuditFact(x.ActorId, x.RecordedAt))
            .Concat(model.ListCorrection.Select(x => new AuditFact(x.CorrectedBy, x.RecordedAt)))
            .OrderBy(x => x.RecordedAt)
            .ToList();
        var first = facts.FirstOrDefault() ?? new AuditFact(string.Empty, EmptyDate);
        var last = facts.LastOrDefault() ?? first;

        return new PakaiBedAlokasiDto(
            model.PakaiBedId, model.Version, model.RegId, model.PasienId,
            model.BangsalId, model.KamarId, model.BedId, model.WaitingListId,
            model.RequestId, (int)model.PakaiBedPurpose, (int)model.OccupantRole,
            (int)model.PakaiBedStatus, model.ProposedAt,
            model.StartedAt ?? EmptyDate, model.AssignedBy,
            model.BedAssignabilityEvidenceId,
            first.UserId, first.RecordedAt, last.UserId, last.RecordedAt,
            string.Empty, EmptyDate);
    }

    public PakaiBedAlokasiModel ToModel(
        IEnumerable<PakaiBedTransisiModel> transitions,
        IEnumerable<PakaiBedKoreksiModel> corrections)
        => new(
            PakaiBedId, Version, RegId, PasienId, BangsalId, KamarId, BedId,
            WaitingListId, RequestId, (PakaiBedPurposeEnum)PakaiBedPurpose,
            (OccupantRoleEnum)OccupantRole, (PakaiBedStatusEnum)PakaiBedStatus,
            AsUtc(ProposedAt), StartedAt.Year == EmptyDate.Year ? null : AsUtc(StartedAt),
            AssignedBy, BedAssignabilityEvidenceId, transitions, corrections);

    private static DateTime AsUtc(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private sealed record AuditFact(string UserId, DateTime RecordedAt);
}
