using Ardalis.GuardClauses;

namespace Bilreg.Domain.LabContext.LabTestDefinitionFeature;

public record LabTestComponentModel
{
    #region CREATION

    public LabTestComponentModel(
        int sequenceNo,
        string componentId,
        string referenceRangeOverride,
        bool requiredFlagging,
        bool isMandatory)
    {
        Guard.Against.OutOfRange(sequenceNo, nameof(sequenceNo), 1, int.MaxValue);
        Guard.Against.NullOrWhiteSpace(componentId, nameof(componentId));

        if (!LabMasterIdFormat.IsValidMlc(componentId))
            throw new ArgumentException(
                "LAB_INVALID_MLC_FORMAT: ComponentId must be MLC + 4 uppercase hex.",
                nameof(componentId));

        SequenceNo = sequenceNo;
        ComponentId = componentId;
        ReferenceRangeOverride = referenceRangeOverride ?? string.Empty;
        RequiredFlagging = requiredFlagging;
        IsMandatory = isMandatory;
    }

    public static LabTestComponentModel Default =>
        new(1, "-", string.Empty, false, false);

    #endregion

    public int SequenceNo { get; init; }
    public string ComponentId { get; init; }
    public string ReferenceRangeOverride { get; init; }
    public bool RequiredFlagging { get; init; }
    public bool IsMandatory { get; init; }
}
