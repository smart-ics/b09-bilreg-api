using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabResultFeature;

namespace Bilreg.Domain.LabContext.LabComponentMasterFeature;

public record LabComponentMasterModel : ILabComponentMasterKey
{
    #region CREATION

    public LabComponentMasterModel(
        string componentId,
        string? loincCode,
        string componentCode,
        string componentName,
        LabResultTypeEnum resultType,
        string defaultUnit,
        bool isSystem,
        bool isActive,
        string? componentNameIndonesia = null)
    {
        Guard.Against.NullOrWhiteSpace(componentId, nameof(componentId));
        Guard.Against.NullOrWhiteSpace(componentCode, nameof(componentCode));
        Guard.Against.NullOrWhiteSpace(componentName, nameof(componentName));

        ComponentId = componentId;
        LoincCode = loincCode;
        ComponentCode = componentCode;
        ComponentName = componentName;
        ComponentNameIndonesia = componentNameIndonesia;
        ResultType = resultType;
        DefaultUnit = defaultUnit ?? string.Empty;
        IsSystem = isSystem;
        IsActive = isActive;
    }

    public static LabComponentMasterModel Default =>
        new("-", null, "-", "-", LabResultTypeEnum.Numeric, string.Empty, false, false);

    public static ILabComponentMasterKey Key(string componentId) =>
        Default with { ComponentId = componentId };

    #endregion

    public string ComponentId { get; init; }
    public string? LoincCode { get; init; }
    public string ComponentCode { get; init; }
    public string ComponentName { get; init; }
    public string? ComponentNameIndonesia { get; init; }
    public LabResultTypeEnum ResultType { get; init; }
    public string DefaultUnit { get; init; }
    public bool IsSystem { get; init; }
    public bool IsActive { get; init; }
}

public interface ILabComponentMasterKey
{
    string ComponentId { get; }
}
