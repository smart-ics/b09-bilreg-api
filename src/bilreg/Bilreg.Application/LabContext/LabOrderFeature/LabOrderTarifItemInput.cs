namespace Bilreg.Application.LabContext.LabOrderFeature;

public record LabOrderTarifItemInput(string TarifId, string? TarifName, int Urgency = 0);
