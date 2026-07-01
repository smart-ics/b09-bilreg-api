using Bilreg.Domain.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabResultFeature;

namespace Bilreg.Infrastructure.LabContext.LabComponentMasterFeature;

public record LabComponentMasterDto(
    string ComponentId,
    string? LoincCode,
    string ComponentCode,
    string ComponentName,
    string? ComponentNameIndonesia,
    int ResultType,
    string DefaultUnit,
    bool IsSystem,
    bool IsActive,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static LabComponentMasterDto FromModel(LabComponentMasterModel model) =>
        new(
            ComponentId: model.ComponentId,
            LoincCode: model.LoincCode,
            ComponentCode: model.ComponentCode,
            ComponentName: model.ComponentName,
            ComponentNameIndonesia: model.ComponentNameIndonesia,
            ResultType: (int)model.ResultType,
            DefaultUnit: model.DefaultUnit,
            IsSystem: model.IsSystem,
            IsActive: model.IsActive,
            CrtUser: string.Empty,
            CrtDate: new DateTime(3000, 1, 1),
            UpdUser: string.Empty,
            UpdDate: new DateTime(3000, 1, 1),
            VodUser: string.Empty,
            VodDate: new DateTime(3000, 1, 1));

    public LabComponentMasterModel ToModel() =>
        new(
            componentId: ComponentId,
            loincCode: LoincCode,
            componentCode: ComponentCode,
            componentName: ComponentName,
            resultType: (LabResultTypeEnum)ResultType,
            defaultUnit: DefaultUnit,
            isSystem: IsSystem,
            isActive: IsActive,
            componentNameIndonesia: ComponentNameIndonesia);
}
