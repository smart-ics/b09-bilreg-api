using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.LabContext.LabMasterFeature;

public record LabDisciplineModel : ILabDisciplineKey
{
    public LabDisciplineModel(
        string disciplineId,
        string disciplineName,
        AuditTrailType auditTrail)
    {
        Guard.Against.NullOrWhiteSpace(disciplineId, nameof(disciplineId));
        Guard.Against.NullOrWhiteSpace(disciplineName, nameof(disciplineName));
        Guard.Against.Null(auditTrail, nameof(auditTrail));

        DisciplineId = disciplineId.Trim();
        DisciplineName = disciplineName.Trim();
        AuditTrail = auditTrail;
    }

    public static LabDisciplineModel Default =>
        new("-", "-", AuditTrailType.Default);

    public static ILabDisciplineKey Key(string disciplineId) =>
        new LabDisciplineModel(disciplineId, "-", AuditTrailType.Default);

    public static LabDisciplineModel Create(
        string disciplineId,
        string disciplineName,
        string userId,
        DateTime createdAt = default)
    {
        Guard.Against.NullOrWhiteSpace(disciplineId, nameof(disciplineId));
        Guard.Against.NullOrWhiteSpace(disciplineName, nameof(disciplineName));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        return new LabDisciplineModel(
            disciplineId.Trim(),
            disciplineName.Trim(),
            AuditTrailType.Create(userId, createdAt));
    }

    public LabDisciplineModel Update(string disciplineName, string userId, DateTime updatedAt = default)
    {
        Guard.Against.NullOrWhiteSpace(disciplineName, nameof(disciplineName));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var updatedAudit = new AuditTrailType(AuditTrail.Created, AuditTrail.Modified, AuditTrail.Voided);
        updatedAudit.Modif(userId, updatedAt);

        return new LabDisciplineModel(
            DisciplineId,
            disciplineName.Trim(),
            updatedAudit);
    }

    public string DisciplineId { get; init; }
    public string DisciplineName { get; init; }
    public AuditTrailType AuditTrail { get; init; }
}
