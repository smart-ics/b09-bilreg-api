namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record QueAntrianEntryActionResponse(
    string AntrianId,
    int NoUrut,
    string PasienTrackerId,
    string Status);
