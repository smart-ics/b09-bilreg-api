namespace Bilreg.Application.AdmisiContext.RegFeature;

public sealed record AdmissionRegistrationQueueContext(
    string AntrianId,
    int NoUrut,
    string LoketKey,
    byte[] ExpectedRowVersion);

public static class AdmissionRegistrationQueueContextResolver
{
    public static bool HasAny(string? antrianId, int? noUrut, string? expectedRowVersion) =>
        !string.IsNullOrWhiteSpace(antrianId) ||
        noUrut.HasValue ||
        !string.IsNullOrWhiteSpace(expectedRowVersion);

    public static AdmissionRegistrationQueueContext? Resolve(
        string? antrianId,
        int? noUrut,
        string? expectedRowVersion,
        string? serverResolvedLoketKey)
    {
        if (!HasAny(antrianId, noUrut, expectedRowVersion))
            return null;

        if (string.IsNullOrWhiteSpace(antrianId) ||
            noUrut is not > 0 ||
            string.IsNullOrWhiteSpace(expectedRowVersion))
        {
            throw new ArgumentException(
                "AdmissionAntrianId, AdmissionNoUrut, and AdmissionExpectedRowVersion must be supplied together.");
        }

        if (string.IsNullOrWhiteSpace(serverResolvedLoketKey))
            throw new ArgumentException("A server-resolved Admission Loket is required.");

        byte[] version;
        try
        {
            version = Convert.FromBase64String(expectedRowVersion);
        }
        catch (FormatException)
        {
            throw new ArgumentException("AdmissionExpectedRowVersion must be Base64.");
        }

        return new AdmissionRegistrationQueueContext(
            antrianId.Trim(),
            noUrut.Value,
            serverResolvedLoketKey.Trim(),
            version);
    }
}
