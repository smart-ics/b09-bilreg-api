using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.LabContext.LabMasterFeature;

public record LabAntibioticModel : ILabAntibioticKey
{
    public LabAntibioticModel(
        string antibioticId,
        string antibioticName,
        string diskConcentration,
        string resistenRange,
        string intermediateRange,
        string sensitifRange,
        int displayOrder,
        AuditTrailType auditTrail)
    {
        Guard.Against.NullOrWhiteSpace(antibioticId, nameof(antibioticId));
        Guard.Against.NullOrWhiteSpace(antibioticName, nameof(antibioticName));
        Guard.Against.Null(auditTrail, nameof(auditTrail));

        AntibioticId = antibioticId.Trim();
        AntibioticName = antibioticName.Trim();
        DiskConcentration = diskConcentration?.Trim() ?? string.Empty;
        ResistenRange = resistenRange?.Trim() ?? string.Empty;
        IntermediateRange = intermediateRange?.Trim() ?? string.Empty;
        SensitifRange = sensitifRange?.Trim() ?? string.Empty;
        DisplayOrder = displayOrder;
        AuditTrail = auditTrail;
    }

    public static LabAntibioticModel Default =>
        new("-", "-", string.Empty, string.Empty, string.Empty, string.Empty, 0, AuditTrailType.Default);

    public static ILabAntibioticKey Key(string antibioticId) =>
        new LabAntibioticModel(antibioticId, "-", string.Empty, string.Empty, string.Empty, string.Empty, 0, AuditTrailType.Default);

    public static LabAntibioticModel Create(
        string antibioticId,
        string antibioticName,
        string diskConcentration,
        string resistenRange,
        string intermediateRange,
        string sensitifRange,
        int displayOrder,
        string userId,
        DateTime createdAt = default)
    {
        Guard.Against.NullOrWhiteSpace(antibioticId, nameof(antibioticId));
        Guard.Against.NullOrWhiteSpace(antibioticName, nameof(antibioticName));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        return new LabAntibioticModel(
            antibioticId.Trim(),
            antibioticName.Trim(),
            diskConcentration?.Trim() ?? string.Empty,
            resistenRange?.Trim() ?? string.Empty,
            intermediateRange?.Trim() ?? string.Empty,
            sensitifRange?.Trim() ?? string.Empty,
            displayOrder,
            AuditTrailType.Create(userId, createdAt));
    }

    public static LabAntibioticModel Create(
        string antibioticId,
        string antibioticName,
        string userId,
        DateTime createdAt = default)
        => Create(antibioticId, antibioticName, string.Empty, string.Empty, string.Empty, string.Empty, 0, userId, createdAt);

    public LabAntibioticModel Update(
        string antibioticName,
        string diskConcentration,
        string resistenRange,
        string intermediateRange,
        string sensitifRange,
        int displayOrder,
        string userId,
        DateTime updatedAt = default)
    {
        Guard.Against.NullOrWhiteSpace(antibioticName, nameof(antibioticName));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var updatedAudit = new AuditTrailType(AuditTrail.Created, AuditTrail.Modified, AuditTrail.Voided);
        updatedAudit.Modif(userId, updatedAt);

        return new LabAntibioticModel(
            AntibioticId,
            antibioticName.Trim(),
            diskConcentration?.Trim() ?? string.Empty,
            resistenRange?.Trim() ?? string.Empty,
            intermediateRange?.Trim() ?? string.Empty,
            sensitifRange?.Trim() ?? string.Empty,
            displayOrder,
            updatedAudit);
    }

    public LabAntibioticModel Update(
        string antibioticName,
        string userId,
        DateTime updatedAt = default)
        => Update(antibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, userId, updatedAt);

    public string AntibioticId { get; init; }
    public string AntibioticName { get; init; }
    public string DiskConcentration { get; init; }
    public string ResistenRange { get; init; }
    public string IntermediateRange { get; init; }
    public string SensitifRange { get; init; }
    public int DisplayOrder { get; init; }
    public AuditTrailType AuditTrail { get; init; }
}
