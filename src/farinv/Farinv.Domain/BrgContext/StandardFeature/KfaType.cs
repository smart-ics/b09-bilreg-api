using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.StandardFeature;

public record KfaType : IKfaKey
{
    #region CREATION
    public KfaType(string kfaId, string kfaName, bool active, string state,
        string kfaTemplateId, string kfaTemplateName, string farmalkesTypeId, string farmalkesTypeName,
        string farmalkesTypeGroup, string farmalkesHsCode, string produksi, string namaDagang,
        string manufacturer, string registrar, string nomorIzinEdar, string lkppCode,
        string controlledDrugId, string controlledDrugName, string rutePemberianId, string rutePemberianName,
        string bentukSediaanId, decimal dosePerUnit, string ucumId, string ucumName,
        string uomName, bool generik, decimal fixPrice, decimal hetPrice,
        string rxTerm, decimal netWeight, string netWeightName, decimal volume,
        string volumeName, string description, string indication, string warning,
        string sideEffect, string tags,    
        KfaReplacementType replacement,
        IEnumerable<KfaIngredientType> listIngredient,
        IEnumerable<KfaDosageUsageType> listDosageUsage,
        IEnumerable<KfaPackagingType> listPackaging
    )
    {
        KfaId = kfaId;
        KfaName = kfaName;
        Active = active;
        State = state;
        KfaTemplateId = kfaTemplateId;
        KfaTemplateName = kfaTemplateName;
        FarmalkesTypeId = farmalkesTypeId;
        FarmalkesTypeName = farmalkesTypeName;
        FarmalkesTypeGroup = farmalkesTypeGroup;
        FarmalkesHsCode = farmalkesHsCode;
        Produksi = produksi;
        NamaDagang = namaDagang;
        Manufacturer = manufacturer;
        Registrar = registrar;
        NomorIzinEdar = nomorIzinEdar;
        LkppCode = lkppCode;
        ControlledDrugId = controlledDrugId;
        ControlledDrugName = controlledDrugName;
        RutePemberianId = rutePemberianId;
        RutePemberianName = rutePemberianName;
        BentukSediaanId = bentukSediaanId;
        DosePerUnit = dosePerUnit;
        UcumId = ucumId;
        UcumName = ucumName;
        UomName = uomName;
        Generik = generik;
        FixPrice = fixPrice;
        HetPrice = hetPrice;
        RxTerm = rxTerm;
        NetWeight = netWeight;
        NetWeightName = netWeightName;
        Volume = volume;
        VolumeName = volumeName;
        Description = description;
        Indication = indication;
        Warning = warning;
        SideEffect = sideEffect;
        Tags = tags;
        Replacement = replacement;
        ListIngredient = listIngredient?.ToList() ?? [];
        ListDosageUsage = listDosageUsage?.ToList() ?? [];
        ListPackaging = listPackaging?.ToList() ?? [];
    }

    public static KfaType Default => new(kfaId: "-", kfaName: "-",
        active: false, state: "-", kfaTemplateId: "-", kfaTemplateName: "-",
        farmalkesTypeId: "-", farmalkesTypeName: "-", farmalkesTypeGroup: "-", farmalkesHsCode: "-",
        produksi: "-", namaDagang: "-", manufacturer: "-", registrar: "-",
        nomorIzinEdar: "-", lkppCode: "-", controlledDrugId: "-", controlledDrugName: "-",
        rutePemberianId: "-", rutePemberianName: "-", bentukSediaanId: "-", dosePerUnit: 0,
        ucumId: "-", ucumName: "-", uomName: "-", generik: false,
        fixPrice: 0, hetPrice: 0, rxTerm: "-", netWeight: 0,
        netWeightName: "-", volume: 0, volumeName: "-", description: "-",
        indication: "-", warning: "-", sideEffect: "-", tags: "-",

        replacement: KfaReplacementType.Default,
        listIngredient: [],
        listDosageUsage: [],
        listPackaging: []
    );

    public static IKfaKey Key(string id) => Default with { KfaId = id };
    #endregion

    #region PROPERTIES
    public string KfaId { get; init; }
    public string KfaName { get; init; }
    public bool Active { get; init; }
    public string State { get; init; }
    public string KfaTemplateId { get; init; }
    public string KfaTemplateName { get; init; }
    public string FarmalkesTypeId { get; init; }
    public string FarmalkesTypeName { get; init; }
    public string FarmalkesTypeGroup { get; init; }
    public string FarmalkesHsCode { get; init; }
    public string Produksi { get; init; }
    public string NamaDagang { get; init; }
    public string Manufacturer { get; init; }
    public string Registrar { get; init; }
    public string NomorIzinEdar { get; init; }
    public string LkppCode { get; init; }
    public string ControlledDrugId { get; init; }
    public string ControlledDrugName { get; init; }
    public string RutePemberianId { get; init; }
    public string RutePemberianName { get; init; }
    public string BentukSediaanId { get; init; }
    public decimal DosePerUnit { get; init; }
    public string UcumId { get; init; }
    public string UcumName { get; init; }
    public string UomName { get; init; }
    public bool Generik { get; init; }
    public decimal FixPrice { get; init; }
    public decimal HetPrice { get; init; }
    public string RxTerm { get; init; }
    public decimal NetWeight { get; init; }
    public string NetWeightName { get; init; }
    public decimal Volume { get; init; }
    public string VolumeName { get; init; }
    public string Description { get; init; }
    public string Indication { get; init; }
    public string Warning { get; init; }
    public string SideEffect { get; init; }
    public string Tags { get; init; }


    public KfaReplacementType Replacement { get; init; }
    public IEnumerable<KfaIngredientType> ListIngredient { get; init; }
    public IEnumerable<KfaDosageUsageType> ListDosageUsage { get; init; }
    public IEnumerable<KfaPackagingType> ListPackaging { get; init; }
    #endregion

    #region BEHAVIOUR
    public KfaReff ToReff() => new(KfaId, KfaName);
    #endregion
}

public interface IKfaKey
{
    string KfaId { get; }
}

public record KfaReff(string KfaId, string KfaName);
