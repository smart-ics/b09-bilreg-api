namespace Bilreg.Application.LabContext.LabComponentMasterFeature;

public record LabComponentMasterListFilter(bool ActiveOnly = true, string? Search = null);
