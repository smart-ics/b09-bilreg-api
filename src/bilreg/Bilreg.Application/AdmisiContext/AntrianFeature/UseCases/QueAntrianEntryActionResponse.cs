namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record QueAntrianEntryActionResponse(
    string AntrianId,
    int NoUrut,
    string PasienTrackerId,
    string Status);
