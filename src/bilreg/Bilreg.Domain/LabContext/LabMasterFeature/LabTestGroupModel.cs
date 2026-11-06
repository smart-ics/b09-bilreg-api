using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.LabContext.LabMasterFeature;

public record LabTestGroupModel : ILabTestGroupKey
{
    public LabTestGroupModel(
        string testGroupId,
        string testGroupName,
        AuditTrailType auditTrail)
    {
        Guard.Against.NullOrWhiteSpace(testGroupId, nameof(testGroupId));
        Guard.Against.NullOrWhiteSpace(testGroupName, nameof(testGroupName));
        Guard.Against.Null(auditTrail, nameof(auditTrail));

        TestGroupId = testGroupId.Trim();
        TestGroupName = testGroupName.Trim();
        AuditTrail = auditTrail;
    }

    public static LabTestGroupModel Default =>
        new("-", "-", AuditTrailType.Default);

    public static ILabTestGroupKey Key(string testGroupId) =>
        new LabTestGroupModel(testGroupId, "-", AuditTrailType.Default);

    public static LabTestGroupModel Create(
        string testGroupId,
        string testGroupName,
        string userId,
        DateTime createdAt = default)
    {
        Guard.Against.NullOrWhiteSpace(testGroupId, nameof(testGroupId));
        Guard.Against.NullOrWhiteSpace(testGroupName, nameof(testGroupName));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        return new LabTestGroupModel(
            testGroupId.Trim(),
            testGroupName.Trim(),
            AuditTrailType.Create(userId, createdAt));
    }

    public LabTestGroupModel Update(string testGroupName, string userId, DateTime updatedAt = default)
    {
        Guard.Against.NullOrWhiteSpace(testGroupName, nameof(testGroupName));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var updatedAudit = new AuditTrailType(AuditTrail.Created, AuditTrail.Modified, AuditTrail.Voided);
        updatedAudit.Modif(userId, updatedAt);

        return new LabTestGroupModel(
            TestGroupId,
            testGroupName.Trim(),
            updatedAudit);
    }

    public string TestGroupId { get; init; }
    public string TestGroupName { get; init; }
    public AuditTrailType AuditTrail { get; init; }
}
