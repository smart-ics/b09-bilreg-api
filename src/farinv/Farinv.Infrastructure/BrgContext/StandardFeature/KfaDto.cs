using Farinv.Domain.BrgContext.StandardFeature;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public record KfaDto(string KfaId, string KfaName, bool Active, string State,
    string KfaTemplateId, string KfaTemplateName, string FarmalkesTypeId, string FarmalkesTypeName,
    string FarmalkesTypeGroup, string FarmalkesHsCode, string Produksi, string NamaDagang,
    string Manufacturer, string Registrar, string NomorIzinEdar, string LkppCode,
    string ControlledDrugId, string ControlledDrugName, string RutePemberianId, string RutePemberianName,
    string BentukSediaanId, decimal DosePerUnit, string UcumId, string UcumName,
    string UomName, bool Generik, decimal FixPrice, decimal HetPrice,
    string RxTerm, decimal NetWeight, string NetWeightName, decimal Volume,
    string VolumeName, string Description, string Indication, string Warning,
    string SideEffect, string Tags
)
{
    public static KfaDto FromModel(KfaType model)
    {
        return new KfaDto(model.KfaId,  model.KfaName, model.Active, model.State,
            model.KfaTemplateId, model.KfaTemplateName, model.FarmalkesTypeId, model.FarmalkesTypeName, 
            model.FarmalkesTypeGroup, model.FarmalkesHsCode, model.Produksi, model.NamaDagang,
            model.Manufacturer, model.Registrar, model.NomorIzinEdar, model.LkppCode,
            model.ControlledDrugId, model.ControlledDrugName, model.RutePemberianId, model.RutePemberianName,
            model.BentukSediaanId, model.DosePerUnit, model.UcumId, model.UcumName,
            model.UomName, model.Generik, model.FixPrice, model.HetPrice, 
            model.RxTerm, model.NetWeight, model.NetWeightName, model.Volume,
            model.VolumeName, model.Description, model.Indication, model.Warning,
            model.SideEffect, model.Tags
        );
    }

    public KfaType ToModel(KfaReplacementType replacement,
        IEnumerable<KfaIngredientType> listIngredient,
        IEnumerable<KfaDosageUsageType> listDosageUsage,
        IEnumerable<KfaPackagingType> listPackaging)
    {
        return new KfaType(KfaId, KfaName, Active, State,
            KfaTemplateId, KfaTemplateName, FarmalkesTypeId, FarmalkesTypeName,
            FarmalkesTypeGroup, FarmalkesHsCode, Produksi, NamaDagang,
            Manufacturer, Registrar, NomorIzinEdar, LkppCode,
            ControlledDrugId, ControlledDrugName, RutePemberianId, RutePemberianName,
            BentukSediaanId, DosePerUnit, UcumId, UcumName,
            UomName, Generik, FixPrice, HetPrice,
            RxTerm, NetWeight, NetWeightName, Volume,
            VolumeName, Description, Indication, Warning,
            SideEffect, Tags, replacement,
            listIngredient, listDosageUsage, listPackaging
        );
    }
}